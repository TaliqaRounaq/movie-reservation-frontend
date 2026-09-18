using CineBook.Models;
using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CineBook.Controllers
{
    public class TheatresController : Controller
    {
        private readonly HttpClient _httpClient;

        public TheatresController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
        }

        public async Task<IActionResult> Index(int? cityId)
        {
            // The navbar link does not carry a query string, so use the city the
            // visitor selected and saved in the browser cookie. A query value takes
            // precedence when the city selector deliberately reloads this page.
            var selectedCityId = cityId is > 0
                ? cityId.GetValueOrDefault()
                : GetSelectedCityId() ?? 2;

            ViewData["SelectedCityId"] = selectedCityId;
            ViewData["SelectedCityName"] = Request.Cookies["cinego-city-name"] ?? "your city";

            List<Theatre> theatres = await FetchTheatresForCityAsync(selectedCityId);
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
            // Step 1: Theatre info
            Theatre? theatre = await FetchTheatreByIdAsync(theatreId);
            if (theatre == null) return NotFound();

            List<DateTime> availableDates = GetNextFourDates();
            DateTime selectedDate = date ?? availableDates[0];

            // Step 2: Is theatre ke screens dhoondo
            List<(int ScreenId, string ScreenName)> screens = await FetchScreensForTheatreAsync(theatreId);

            // Step 3: Har screen ke shows fetch karo, combine karo
            var allShows = new List<(int ShowId, int MovieId, DateTime ShowTime, string ScreenName)>();
            foreach (var screen in screens)
            {
                var shows = await FetchShowsForScreenAsync(screen.ScreenId);
                foreach (var s in shows)
                {
                    allShows.Add((s.ShowId, s.MovieId, s.ShowTime, screen.ScreenName));
                }
            }

            // Step 4: Selected date ke shows filter karo
            var showsOnDate = allShows.Where(s => s.ShowTime.Date == selectedDate.Date).ToList();

            // Step 5: Movie-wise group karo
            var movieGroups = new List<MovieWithShowtimes>();
            foreach (var group in showsOnDate.GroupBy(s => s.MovieId))
            {
                Movie? movie = await FetchMovieAsync(group.Key);
                if (movie == null) continue;

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
                Movies = movieGroups
            };

            return View(viewModel);
        }

        private async Task<List<Theatre>> FetchTheatresForCityAsync(int cityId)
        {
            var response = await _httpClient.GetAsync($"api/theaters/city/{cityId}");
            if (!response.IsSuccessStatusCode) return new List<Theatre>();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return new List<Theatre>();

            return wrapper.Data.Select(t => new Theatre { Id = t.TheaterId, Name = t.TheaterName, Area = t.Location }).ToList();
        }

        private async Task<Theatre?> FetchTheatreByIdAsync(int theatreId)
        {
            var response = await _httpClient.GetAsync("api/theaters");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return null;

            var match = wrapper.Data.FirstOrDefault(t => t.TheaterId == theatreId);
            if (match == null) return null;

            return new Theatre { Id = match.TheaterId, Name = match.TheaterName, Area = match.Location };
        }

        //private async Task<List<(int ScreenId, string ScreenName)>> FetchScreensForTheatreAsync(int theatreId)
        private async Task<List<(int ScreenId, string ScreenName)>> FetchScreensForTheatreAsync(int theatreId)
        {
            var response = await _httpClient.GetAsync($"api/theaters/{theatreId}/screens");
            if (!response.IsSuccessStatusCode) return new List<(int, string)>();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterScreenDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return new List<(int, string)>();

            return wrapper.Data
                .Select(s => (s.ScreenId, s.ScreenName))
                .ToList();
        }

        private async Task<List<(int ShowId, int MovieId, DateTime ShowTime)>> FetchShowsForScreenAsync(int screenId)
        {
            var response = await _httpClient.GetAsync($"api/screens/{screenId}/shows");
            if (!response.IsSuccessStatusCode) return new List<(int, int, DateTime)>();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<ScreenShowDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return new List<(int, int, DateTime)>();

            return wrapper.Data.Select(s => (s.ShowId, s.MovieId, s.ShowTime)).ToList();
        }

        private async Task<Movie?> FetchMovieAsync(int movieId)
        {
            var response = await _httpClient.GetAsync($"api/movies/{movieId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
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
