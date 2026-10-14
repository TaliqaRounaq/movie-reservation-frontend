using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using CineBook.Models;
using CineBook.Models.DTOs;
using System.Text.Json;

namespace CineBook.Controllers

{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var cityIdCookie = Request.Cookies["cinego-city-id"];
            int? cityId = int.TryParse(cityIdCookie, out var parsedCityId) ? parsedCityId : null;

            var model = await LoadHomeModelAsync(cityId);
            ViewData["SelectedCityName"] = Request.Cookies["cinego-city-name"];
            return View(model);
        }

        private static readonly List<string> AllGenres = new()
        {
            "Action", "Comedy", "Drama", "Thriller", "Horror", "Romance", "Sci-Fi", "Animation"
        };

        private static readonly List<string> AllLanguages = new()
        {
            "Hindi", "English", "Urdu", "Kashmiri", "Punjabi"
        };

        private async Task<List<MovieListDto>> FilterMoviesAsync(string genre, string language)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(genre)) queryParams.Add($"genre={Uri.EscapeDataString(genre)}");
            if (!string.IsNullOrEmpty(language)) queryParams.Add($"language={Uri.EscapeDataString(language)}");

            var response = await _httpClient.GetAsync($"/api/movies/filter?{string.Join("&", queryParams)}");

            if (!response.IsSuccessStatusCode)
                return new List<MovieListDto>();

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<MovieListDto>>>();
            return wrapper?.Data ?? new List<MovieListDto>();
        }

        private async Task<List<MovieListDto>> SearchMoviesAsync(string keyword)
        {
            var response = await _httpClient.GetAsync($"/api/movies/search?keyword={Uri.EscapeDataString(keyword)}");

            if (!response.IsSuccessStatusCode)
                return new List<MovieListDto>();

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<MovieListDto>>>();
            return wrapper?.Data ?? new List<MovieListDto>();
        }
        private async Task<List<MovieListDto>> FetchMoviesByCityAsync(int cityId)
        {
            var response = await _httpClient.GetAsync($"api/movies/city/{cityId}");

            if (!response.IsSuccessStatusCode)
                return new List<MovieListDto>();

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<List<MovieListDto>>>();
            return wrapper?.Data ?? new List<MovieListDto>();
        }

        private async Task<List<Theatre>> FetchAllTheatresAsync()
        {
            var theatres = new List<Theatre>();

            var response = await _httpClient.GetAsync("api/theaters");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

                if (wrapper?.Success == true && wrapper.Data != null)
                {
                    foreach (var dto in wrapper.Data)
                    {
                        theatres.Add(new Theatre
                        {
                            Id = dto.TheaterId,
                            Name = dto.TheaterName,
                            Area = dto.Location
                        });
                    }
                }
            }

            return theatres;
        }
        private async Task<List<Theatre>> FetchTheatresByCityAsync(int cityId)
        {
            var theatres = new List<Theatre>();

            var response = await _httpClient.GetAsync($"api/theaters/city/{cityId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<TheaterListDto>>>(json, options);

                if (wrapper?.Success == true && wrapper.Data != null)
                {
                    foreach (var dto in wrapper.Data)
                    {
                        theatres.Add(new Theatre
                        {
                            Id = dto.TheaterId,
                            Name = dto.TheaterName,
                            Area = dto.Location
                        });
                    }
                }
            }

            return theatres;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? q, string? genre, string? language)
        {
            var query = q?.Trim() ?? string.Empty;
            var selectedGenre = genre?.Trim() ?? string.Empty;
            var selectedLanguage = language?.Trim() ?? string.Empty;

            var cityIdCookie = Request.Cookies["cinego-city-id"];
            int? cityId = int.TryParse(cityIdCookie, out var parsedCityId) ? parsedCityId : null;

            var hasKeyword = !string.IsNullOrEmpty(query);
            var hasFilter = !string.IsNullOrEmpty(selectedGenre) || !string.IsNullOrEmpty(selectedLanguage);

            List<MovieListDto> searchResults;

            if (hasKeyword)
            {
                searchResults = await SearchMoviesAsync(query);

                if (hasFilter)
                {
                    searchResults = searchResults
                        .Where(m =>
                            (string.IsNullOrEmpty(selectedGenre) || m.Genre.Equals(selectedGenre, StringComparison.OrdinalIgnoreCase)) &&
                            (string.IsNullOrEmpty(selectedLanguage) || m.Language.Equals(selectedLanguage, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            }
            else if (hasFilter)
            {
                searchResults = await FilterMoviesAsync(selectedGenre, selectedLanguage);
            }
            else
            {
                searchResults = new List<MovieListDto>();
            }
            if (cityId.HasValue)
            {
                var cityMovies = await FetchMoviesByCityAsync(cityId.Value);
                var cityMovieIds = cityMovies.Select(m => m.MovieId).ToHashSet();

                searchResults = searchResults
                    .Where(m => cityMovieIds.Contains(m.MovieId))
                    .ToList();
            }

            var movies = searchResults.Select(m => new Movie
            {
                Id = m.MovieId,
                Title = m.Title,
                Genre = m.Genre,
                Language = m.Language,
                Rating = (double)m.Rating,
                Runtime = FormatDuration(m.Duration),
                PosterUrl = m.PosterUrl
            }).ToList();

            var cityTheatres = cityId.HasValue
            ? await FetchTheatresByCityAsync(cityId.Value)
            : new List<Theatre>();

            var theatres = cityTheatres.Where(theatre =>
                        string.IsNullOrEmpty(query) ||
                        theatre.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        theatre.Area.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .ToList();

            var model = new MovieSearchViewModel
            {
                Query = query,
                Genre = selectedGenre,
                Language = selectedLanguage,
                Movies = movies,
                Theatres = theatres,
                Genres = AllGenres,
                Languages = AllLanguages
            };

            return View(model);
        }

        private async Task<HomeViewModel> LoadHomeModelAsync(int? cityId)
        {
            var errors = new List<string>();
            var nowShowingTask = FetchMoviesAsync("api/movies/now-showing",errors);
            var recommendedTask = FetchMoviesAsync("api/movies/recommended",errors);
            var upcomingTask = FetchMoviesAsync("api/movies/upcoming",errors);
            var nearbyTheatresTask = cityId.HasValue
                ? FetchTheatresByCityAsync(cityId.Value)
                : Task.FromResult(new List<Theatre>());

            await Task.WhenAll(nowShowingTask, recommendedTask, upcomingTask, nearbyTheatresTask);
            var nowShowing = nowShowingTask.Result;

            var heroMovies = nowShowing.Where(m => m.IsFeatured).Take(4).ToList();
            if (!heroMovies.Any())
            {
                heroMovies = nowShowing.Take(4).ToList();
            }

            return new HomeViewModel
            {
                HeroMovies = heroMovies,
                NowShowing = nowShowing,
                Recommended = recommendedTask.Result,
                Upcoming = upcomingTask.Result,
                NearbyTheatres = nearbyTheatresTask.Result,
                Genres = AllGenres,
                Actors = new List<string> { "Shah Rukh Khan", "Alia Bhatt", "Ranbir Kapoor" },
                Languages = AllLanguages ,
                ErrorMessages = errors.Distinct().ToList()
            };
        }

        private async Task<List<Movie>> FetchMoviesAsync(string endpoint, List<string> errors)
        {
            var movies = new List<Movie>();

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Backend returned {StatusCode} for {Endpoint}",
                        response.StatusCode, endpoint);
                    errors.Add("Some movies couldn't be loaded right now.");
                    return movies;   
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<MovieListDto>>>(json, options);

                if (wrapper?.Success == true && wrapper.Data != null)
                {
                    foreach (var dto in wrapper.Data)
                    {
                        movies.Add(new Movie
                        {
                            Id = dto.MovieId,
                            Title = dto.Title,
                            Genre = dto.Genre,
                            Language = dto.Language,
                            Rating = (double)dto.Rating,
                            Runtime = FormatDuration(dto.Duration),
                            PosterUrl = dto.PosterUrl,
                            IsFeatured = dto.IsFeatured
                        });
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timed out calling {Endpoint}", endpoint);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling {Endpoint}", endpoint);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from {Endpoint}", endpoint);
            }

            return movies;
        }

        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var cities = new List<CityDto>();

            var response = await _httpClient.GetAsync("api/cities");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<CityDto>>>(json, options);

                if (wrapper?.Success == true && wrapper.Data != null)
                {
                    cities = wrapper.Data;
                }
            }

            return Json(cities);
        }

        [HttpGet]
        public async Task<IActionResult> GetTheatresByCity(int cityId)
        {
            var theatres = await FetchTheatresByCityAsync(cityId);
            return Json(theatres);
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return string.Empty;
            var hours = minutes / 60;
            var mins = minutes % 60;
            return $"{hours}h {mins:D2}m";
        }

        public IActionResult Error(int? statusCode = null)
        {
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionFeature?.Error;

            if (exception != null)
            {
                // Path 1: a real exception was thrown somewhere
                _logger.LogError(
                    exception,
                    "Unhandled exception on {Path}",
                    exceptionFeature?.Path);

                ViewData["StatusCode"] = 500;
            }
            else if (statusCode.HasValue)
            {
                // Path 2: a status-code page (404, 403, etc.), no exception
                _logger.LogWarning(
                    "Status code page triggered: {StatusCode} for {Path}",
                    statusCode.Value,
                    HttpContext.Request.Path);

                ViewData["StatusCode"] = statusCode.Value;
            }
            else
            {
                // Fallback — shouldn't normally happen, but keeps the view safe
                ViewData["StatusCode"] = 500;
            }

            return View();
        }
    }
}