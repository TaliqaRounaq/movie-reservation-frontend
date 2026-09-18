using Microsoft.AspNetCore.Mvc;
using CineBook.Models;
using CineBook.Models.DTOs;
using System.Text.Json;

namespace CineBook.Controllers
{
    public class MoviesController : Controller
    {
        private readonly HttpClient _httpClient;

        public MoviesController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
        }
        private async Task<List<TheatreAvailability>> FetchShowtimesAsync(int movieId, int? cityId)
        {
            var url = cityId.HasValue
                ? $"api/shows/movie/{movieId}/city/{cityId.Value}"
                : $"api/shows/movie/{movieId}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<TheatreAvailability>();

            var json = await response.Content.ReadAsStringAsync();
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
        [HttpGet]
        public async Task<IActionResult> GetShowtimesByCity(int movieId, int cityId)
        {
            var availability = await FetchShowtimesAsync(movieId, cityId);
            return Json(availability);
        }



        public async Task<IActionResult> Details(int id)
        {
            var movie = await FetchMovieDetailAsync(id);

            if (movie == null)
            {
                return NotFound();
            }
            int? cityId = null;
            if (Request.Cookies.TryGetValue("cinego-city-id", out var cityIdRaw) &&
                int.TryParse(cityIdRaw, out var parsedCityId))
            {
                cityId = parsedCityId;
            }

            var availability = await FetchShowtimesAsync(id, cityId);
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
               
                Availability = availability
            
            };

            return View(model);
        }

        private async Task<MovieDetailDto?> FetchMovieDetailAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/movies/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<MovieDetailDto>>(json, options);

            return wrapper?.Success == true ? wrapper.Data : null;
        }
        private static string FormatRoleName(CineBook.Models.Enums.CrewRole role)
        {
            // "MusicDirector" → "Music Director" (spaces add karne ke liye)
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
