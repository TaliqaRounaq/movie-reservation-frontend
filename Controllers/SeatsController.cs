using CineBook.Models;
using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;   // ← add if not already there
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
namespace CineBook.Controllers
{
    [Authorize]
    public class SeatsController : Controller
    {
        private readonly HttpClient _httpClient;

        public SeatsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
        }

        [HttpGet]
        public async Task<IActionResult> SeatCount(int showtimeId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            {
                var returnUrl = Url.Action(nameof(SeatCount), new { showtimeId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            var show = await FetchShowByIdAsync(showtimeId);
            if (show == null) return NotFound();

            return View(new SeatCountViewModel
            {
                ShowtimeId = show.ShowId,
                MovieName = show.MovieTitle,
                TheatreName = show.TheaterName,
                ScreenName = show.ScreenName,
                ShowDate = show.ShowDateTime.ToString("dd MMM yyyy"),
                ShowTime = show.ShowDateTime.ToString("h:mm tt")
            });
        }

        [HttpGet]
        public async Task<IActionResult> Index(int showtimeId, int? seatCount, string? preSelected)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")))
            {
                var returnUrl = Url.Action(nameof(SeatCount), new { showtimeId });
                return RedirectToAction("Login", "Account", new { returnUrl });
            }

            if (!seatCount.HasValue || seatCount < 1 || seatCount > 10)
                return RedirectToAction(nameof(SeatCount), new { showtimeId });

            // Step A: Show ka detail fetch karo (movie/theatre/date/time/screen)
            ShowListDto? show = await FetchShowByIdAsync(showtimeId);
            if (show == null) return NotFound();

            // Step B: Is show ke real seats fetch karo
            List<Seat> seats = await FetchSeatsForShowAsync(showtimeId);
            var currentUserReservedSeats = GetReservedSeatIds(showtimeId);

            if (!string.IsNullOrEmpty(preSelected))
            {
                var preSelectedIds = preSelected
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToHashSet();

                foreach (var seat in seats)
                {
                    if (preSelectedIds.Contains(seat.ShowSeatId) &&
                        currentUserReservedSeats.Contains(seat.ShowSeatId) &&
                        seat.Status == SeatStatus.Reserved)
                        seat.IsSelected = true;
                }
            }

            foreach (var seat in seats.Where(s => currentUserReservedSeats.Contains(s.ShowSeatId) && s.Status == SeatStatus.Reserved))
            {
                seat.IsSelected = true;
            }

            var viewModel = new SeatSelectionViewModel
            {
                ShowtimeId = show.ShowId,
                SeatCount = seatCount.Value,
                MovieName = show.MovieTitle,
                TheatreName = show.TheaterName,
                ShowDate = show.ShowDateTime.ToString("dd MMM yyyy"),
                ShowTime = show.ShowDateTime.ToString("h:mm tt"),
                ScreenName = show.ScreenName,

                Seats = seats,
                Total = 0
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Reserve([FromBody] SeatActionDto request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { success = false, message = "Please log in to select a seat." });
            System.Diagnostics.Debug.WriteLine($"[RESERVE] ShowSeatId={request.ShowSeatId} SessionId={HttpContext.Session.Id} JWT={DecodeJwtPayload(token)}");
            var req = new HttpRequestMessage(HttpMethod.Post, "api/seats/reserve");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new { showSeatId = request.ShowSeatId });

            var response = await _httpClient.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            if (IsSuccessfulResponse(json))
            {
                var reservedSeats = GetReservedSeatIds(request.ShowtimeId);
                reservedSeats.Add(request.ShowSeatId);
                SaveReservedSeatIds(request.ShowtimeId, reservedSeats);
            }
            return Content(json, "application/json");
        }

        [HttpPost]
        public async Task<IActionResult> Release([FromBody] SeatActionDto request)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { success = false, message = "Please log in first." });

            var req = new HttpRequestMessage(HttpMethod.Post, "api/seats/release");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new { showSeatId = request.ShowSeatId });

            var response = await _httpClient.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();
            if (IsSuccessfulResponse(json))
            {
                var reservedSeats = GetReservedSeatIds(request.ShowtimeId);
                reservedSeats.Remove(request.ShowSeatId);
                SaveReservedSeatIds(request.ShowtimeId, reservedSeats);
            }
            return Content(json, "application/json");
        }

        private HashSet<int> GetReservedSeatIds(int showtimeId)
        {
            var json = HttpContext.Session.GetString($"ReservedSeats:{showtimeId}");
            return string.IsNullOrEmpty(json)
                ? new HashSet<int>()
                : JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
        }

        private void SaveReservedSeatIds(int showtimeId, HashSet<int> reservedSeats) =>
            HttpContext.Session.SetString($"ReservedSeats:{showtimeId}", JsonSerializer.Serialize(reservedSeats));

        private static bool IsSuccessfulResponse(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private async Task<ShowListDto?> FetchShowByIdAsync(int showtimeId)
        {
            var response = await _httpClient.GetAsync("api/shows");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<ShowListDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return null;
            return wrapper.Data.FirstOrDefault(s => s.ShowId == showtimeId);
        }

        private async Task<List<Seat>> FetchSeatsForShowAsync(int showtimeId)
        {
            var response = await _httpClient.GetAsync($"api/seats/show/{showtimeId}");
            if (!response.IsSuccessStatusCode) return new List<Seat>();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<AvailableSeatDto>>>(json, options);

            if (wrapper?.Success != true || wrapper.Data == null) return new List<Seat>();

            return wrapper.Data.Select(dto => new Seat
            {
                ShowSeatId = dto.ShowSeatId,
                SeatId = dto.SeatId,
                Row = dto.SeatNumber.Length > 0 ? dto.SeatNumber[..1] : "?",
                Number = int.TryParse(dto.SeatNumber.Length > 1 ? dto.SeatNumber[1..] : "0", out var num) ? num : 0,
                Tier = System.Enum.TryParse<PricingTier>(dto.SeatType, true, out var tier) ? tier : PricingTier.Regular,
                Status = System.Enum.TryParse<SeatStatus>(dto.Status, true, out var status) ? status : SeatStatus.Available,
                IsSelected = false
            }).ToList();
        }
        private static string DecodeJwtPayload(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return "invalid token";

                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                return $"decode failed: {ex.Message}";
            }
        }
    }
}
