using CineBook.Models;
using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace CineBook.Controllers
{
    public class TheatresController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TheatresController> _logger;

        public TheatresController(IHttpClientFactory httpClientFactory, ILogger<TheatresController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
            _logger = logger;
        }

        private const string GenericLoadError = "Some content couldn't be loaded right now. Please try again shortly.";

        private async Task<string?> SafeGetJsonAsync(string endpoint, List<string> errors)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Backend returned {StatusCode} for {Endpoint}", response.StatusCode, endpoint);
                    errors.Add(GenericLoadError);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timed out calling {Endpoint}", endpoint);
                errors.Add(GenericLoadError);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling {Endpoint}", endpoint);
                errors.Add(GenericLoadError);
            }

            return null;
        }

        public async Task<IActionResult> Index(int? cityId)
        {
            var errors = new List<string>();

            var selectedCityId = cityId is > 0
                ? cityId.GetValueOrDefault()
                : GetSelectedCityId() ?? 2;

            ViewData["SelectedCityId"] = selectedCityId;
            ViewData["SelectedCityName"] = Request.Cookies["cinego-city-name"] ?? "your city";
            ViewData["ErrorMessages"] = errors;   // NEW — Index() uses a plain List<Theatre> as its model,
                                                  // so ErrorMessages has to travel via ViewData instead of a ViewModel

            List<Theatre> theatres = await FetchTheatresForCityAsync(selectedCityId, errors);
            return View(theatres);
        }

        private int? GetSelectedCityId()
        {
            return Request.Cookies.TryGetValue("cinego-city-id", out var cityIdRaw) &&
                   int.TryParse(cityIdRaw, out var cityId) && cityId > 0
                ? cityId
                : null;
        }

        public async Task<IActionResult> Details(int theatreId, DateTime? date = null)
        {
            var errors = new List<string>();

            // Step 1: Theatre info — this one IS fatal to the page if it fails,
            // so we distinguish "genuinely doesn't exist" from "backend failed".
            var (theatre, notFoundInBackend, theatreFetchFailed) = await FetchTheatreByIdAsync(theatreId, errors);

            if (notFoundInBackend)
            {
                return NotFound();
            }

            if (theatreFetchFailed || theatre == null)
            {
                _logger.LogWarning("Theatre details failed to load for id {TheatreId}", theatreId);
                return RedirectToAction("Error", "Home");
            }

            List<DateTime> availableDates = GetNextFourDates();
            DateTime selectedDate = date ?? availableDates[0];

            // Step 2: Screens — NOT fatal. If this fails, the page still loads,
            // just with an empty movie list for this theatre (handled gracefully below).
            List<(int ScreenId, string ScreenName)> screens = await FetchScreensForTheatreAsync(theatreId, errors);

            // Step 3: Shows per screen — each screen's failure is isolated. One bad
            // screen doesn't take down the others.
            var allShows = new List<(int ShowId, int MovieId, DateTime ShowTime, string ScreenName)>();
            foreach (var screen in screens)
            {
                var shows = await FetchShowsForScreenAsync(screen.ScreenId, errors);
                foreach (var s in shows)
                {
                    allShows.Add((s.ShowId, s.MovieId, s.ShowTime, screen.ScreenName));
                }
            }

            var showsOnDate = allShows.Where(s => s.ShowTime.Date == selectedDate.Date).ToList();

            // Step 4/5: Movie-wise group + fetch — each movie's failure is isolated too.
            var movieGroups = new List<MovieWithShowtimes>();
            foreach (var group in showsOnDate.GroupBy(s => s.MovieId))
            {
                Movie? movie = await FetchMovieAsync(group.Key, errors);
                if (movie == null) continue;   // skip just this movie, keep going

                var showtimes = group
                    .OrderBy(s => s.ShowTime)
                    .Select(s => new Showtime
                    {
                        Id = s.ShowId,
                        MovieId = s.MovieId,
                        TheatreId = theatreId,
                        StartTime = s.ShowTime,
                        Screen = s.ScreenName
                    })
                    .ToList();

                movieGroups.Add(new MovieWithShowtimes { Movie = movie, Showtimes = showtimes });
            }

            var viewModel = new TheatreDetailsViewModel
            {
                Theatre = theatre,
                AvailableDates = availableDates,
                SelectedDate = selectedDate,
                Movies = movieGroups,
                ErrorMessages = errors.Distinct().ToList()   // NEW
            };

            return View(viewModel);
        }

        private async Task<List<Theatre>> FetchTheatresForCityAsync(int cityId, List<string> errors)
        {
            var json = await SafeGetJsonAsync($"api/theaters/city/{cityId}", errors);
            if (json == null) return new List<Theatre>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return new List<Theatre>();

                return wrapper.Data.Select(t => new Theatre { Id = t.TheaterId, Name = t.TheaterName, Area = t.Location }).ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/theaters/city/{CityId}", cityId);
                errors.Add(GenericLoadError);
                return new List<Theatre>();
            }
        }

        private async Task<(Theatre? Theatre, bool NotFoundInBackend, bool Failed)> FetchTheatreByIdAsync(int theatreId, List<string> errors)
        {
            var json = await SafeGetJsonAsync("api/theaters", errors);
            if (json == null) return (null, false, true);

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null)
                    return (null, false, true);

                var match = wrapper.Data.FirstOrDefault(t => t.TheaterId == theatreId);
                if (match == null)
                    return (null, true, false);   // genuinely doesn't exist in the list — not a failure

                return (new Theatre { Id = match.TheaterId, Name = match.TheaterName, Area = match.Location }, false, false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/theaters");
                errors.Add(GenericLoadError);
                return (null, false, true);
            }
        }

        private async Task<List<(int ScreenId, string ScreenName)>> FetchScreensForTheatreAsync(int theatreId, List<string> errors)
        {
            var json = await SafeGetJsonAsync($"api/theaters/{theatreId}/screens", errors);
            if (json == null) return new List<(int, string)>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterScreenDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return new List<(int, string)>();

                return wrapper.Data.Select(s => (s.ScreenId, s.ScreenName)).ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/theaters/{TheatreId}/screens", theatreId);
                errors.Add(GenericLoadError);
                return new List<(int, string)>();
            }
        }

        private async Task<List<(int ShowId, int MovieId, DateTime ShowTime)>> FetchShowsForScreenAsync(int screenId, List<string> errors)
        {
            var json = await SafeGetJsonAsync($"api/screens/{screenId}/shows", errors);
            if (json == null) return new List<(int, int, DateTime)>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<ScreenShowDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return new List<(int, int, DateTime)>();

                return wrapper.Data.Select(s => (s.ShowId, s.MovieId, s.ShowTime)).ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/screens/{ScreenId}/shows", screenId);
                errors.Add(GenericLoadError);
                return new List<(int, int, DateTime)>();
            }
        }

        private async Task<Movie?> FetchMovieAsync(int movieId, List<string> errors)
        {
            var json = await SafeGetJsonAsync($"api/movies/{movieId}", errors);
            if (json == null) return null;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<MovieDetailDto>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return null;
                var m = wrapper.Data;

                return new Movie
                {
                    Id = movieId,
                    Title = m.Title,
                    Genre = m.Genre,
                    Language = m.Language,
                    Runtime = FormatDuration(m.Duration),
                    Certificate = m.Certificate,
                    Rating = (double)m.Rating,
                    PosterUrl = m.PosterUrl
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/movies/{MovieId}", movieId);
                errors.Add(GenericLoadError);
                return null;
            }
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return string.Empty;
            var hours = minutes / 60;
            var mins = minutes % 60;
            return $"{hours}h {mins:D2}m";
        }

        private List<DateTime> GetNextFourDates()
        {
            var dates = new List<DateTime>();
            for (int i = 0; i < 4; i++) dates.Add(DateTime.Today.AddDays(i));
            return dates;
        }
    }
}