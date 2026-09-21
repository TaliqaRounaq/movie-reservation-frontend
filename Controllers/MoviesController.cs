using Microsoft.AspNetCore.Mvc;
using CineBook.Models;
using CineBook.Models.DTOs;
using System.Net;
using System.Text.Json;

namespace CineBook.Controllers
{
    public class MoviesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IHttpClientFactory httpClientFactory, ILogger<MoviesController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
            _logger = logger;
        }

        private const string GenericLoadError = "Some content couldn't be loaded right now. Please try again shortly.";

        // Same shared helper pattern as HomeController — for calls where a failure
        // just means "show an empty section", not "stop the whole page".
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

        private async Task<List<TheatreAvailability>> FetchShowtimesAsync(int movieId, int? cityId, List<string> errors)
        {
            var url = cityId.HasValue
                ? $"api/shows/movie/{movieId}/city/{cityId.Value}"
                : $"api/shows/movie/{movieId}";

            var json = await SafeGetJsonAsync(url, errors);
            if (json == null) return new List<TheatreAvailability>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<ShowListDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null)
                    return new List<TheatreAvailability>();

                return wrapper.Data
                    .GroupBy(s => new { s.TheaterName, Date = s.ShowDateTime.Date })
                    .Select(g => new TheatreAvailability
                    {
                        TheatreName = g.Key.TheaterName,
                        Date = g.Key.Date.ToString("dd MMM yyyy"),
                        Times = g.OrderBy(s => s.ShowDateTime)
                         .Select(s => new ShowtimeSlot
                         {
                             ShowId = s.ShowId,
                             Time = s.ShowDateTime.ToString("h:mm tt"),
                             ScreenName = s.ScreenName
                         })
                         .ToList()
                    })
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.TheatreName)
                    .ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from {Endpoint}", url);
                errors.Add(GenericLoadError);
                return new List<TheatreAvailability>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShowtimesByCity(int movieId, int cityId)
        {
            // AJAX endpoint hit by movie-details.js on city change — caller just wants
            // whatever list comes back, empty on failure is fine, no error banner needed here.
            var errors = new List<string>();
            var availability = await FetchShowtimesAsync(movieId, cityId, errors);
            return Json(availability);
        }

        public async Task<IActionResult> Details(int id)
        {
            var errors = new List<string>();
            var (movie, notFoundInBackend, movieFetchFailed) = await FetchMovieDetailAsync(id);

            if (notFoundInBackend)
            {
                // Genuine 404 — this movie ID really doesn't exist. Normal navigation case.
                return NotFound();
            }

            if (movieFetchFailed || movie == null)
            {
                // Backend was down/timed out/gave bad JSON — NOT the same as "doesn't exist".
                // Route through the global error page instead of lying with a 404.
                _logger.LogWarning("Movie details failed to load for id {MovieId}", id);
                return RedirectToAction("Error", "Home");
            }

            int? cityId = null;
            if (Request.Cookies.TryGetValue("cinego-city-id", out var cityIdRaw) &&
                int.TryParse(cityIdRaw, out var parsedCityId))
            {
                cityId = parsedCityId;
            }

            // Showtimes failing is NOT fatal for this page — movie details already loaded,
            // so we degrade gracefully: show the movie, with an empty/error-flagged showtimes section.
            var availability = await FetchShowtimesAsync(id, cityId, errors);

            var model = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Runtime = FormatDuration(movie.Duration),
                Certificate = movie.Certificate,
                Genre = movie.Genre,
                Language = movie.Language,
                Rating = (double)movie.Rating,
                PosterUrl = movie.PosterUrl,
                AboutText = movie.Description,
                Cast = movie.Actors.Select(a => a.Name).ToList(),
                Crew = movie.Crew
                    .Select(c => $"{FormatRoleName(c.Role)}: {c.Name}")
                    .ToList(),
                Availability = availability,
                ErrorMessages = errors.Distinct().ToList()   // NEW
            };

            return View(model);
        }

        private async Task<(MovieDetailDto? Movie, bool NotFoundInBackend, bool Failed)> FetchMovieDetailAsync(int id)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync($"api/movies/{id}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timed out calling api/movies/{Id}", id);
                return (null, false, true);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling api/movies/{Id}", id);
                return (null, false, true);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (null, true, false);   // genuinely doesn't exist — not a failure
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Backend returned {StatusCode} for api/movies/{Id}", response.StatusCode, id);
                return (null, false, true);
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<MovieDetailDto>>(json, options);

                if (wrapper?.Success == true && wrapper.Data != null)
                    return (wrapper.Data, false, false);

                return (null, false, true);   // backend said "not success" without a real 404
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/movies/{Id}", id);
                return (null, false, true);
            }
        }

        private static string FormatRoleName(CineBook.Models.Enums.CrewRole role)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                role.ToString(), "([a-z])([A-Z])", "$1 $2"
            );
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return string.Empty;
            var hours = minutes / 60;
            var mins = minutes % 60;
            return $"{hours}h {mins:D2}m";
        }
    }
}