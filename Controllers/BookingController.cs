using CineBook.Models;
using Microsoft.AspNetCore.Authorization;
using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CineBook.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingController> _logger;

        public BookingController(IHttpClientFactory httpClientFactory, ILogger<BookingController> logger)
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

        [HttpGet]
        public async Task<IActionResult> Summary(int showtimeId, string showSeatIds)
        {
            if (string.IsNullOrEmpty(showSeatIds))
            {
                return RedirectToAction("Index", "Seats", new { showtimeId });
            }

            List<int> selectedIds;
            try
            {
                selectedIds = showSeatIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToList();
            }
            catch (FormatException)
            {
                // Someone tampered with the URL / seat IDs are malformed — not a backend
                // problem, so send them back to reselect seats rather than showing a 500.
                _logger.LogWarning("Invalid showSeatIds format for show {ShowtimeId}: {ShowSeatIds}", showtimeId, showSeatIds);
                return RedirectToAction("Index", "Seats", new { showtimeId });
            }

            var errors = new List<string>();

            var (show, showFailed) = await FetchShowByIdAsync(showtimeId, errors);

            if (showFailed)
            {
                // Backend call itself failed — not the same as "show doesn't exist".
                _logger.LogWarning("Show lookup failed for showtimeId {ShowtimeId}", showtimeId);
                return RedirectToAction("Error", "Home");
            }

            if (show == null)
            {
                // Call succeeded, but genuinely no such show — real 404.
                return NotFound();
            }

            List<Seat> allSeats = await FetchSeatsForShowAsync(showtimeId, errors);
            List<Seat> selectedSeats = allSeats.Where(s => selectedIds.Contains(s.ShowSeatId)).ToList();

            if (selectedSeats.Count == 0)
            {
                if (errors.Any())
                {
                    // Seats list came back empty because the call failed, not because
                    // the seat IDs were actually invalid — don't blame the user for this.
                    _logger.LogWarning("Seat lookup failed for showtimeId {ShowtimeId}", showtimeId);
                    return RedirectToAction("Error", "Home");
                }

                return BadRequest("No valid seats selected.");
            }

            var selectedSeatInfos = selectedSeats.Select(s => new SelectedSeatInfo
            {
                ShowSeatId = s.ShowSeatId,
                Row = s.Row,
                Number = s.Number,
                Tier = s.Tier,
                Price = SeatPricing.GetPrice(s.Tier)
            }).ToList();

            var viewModel = new BookingSummaryViewModel
            {
                ShowtimeId = show.ShowId,
                ScreenName = show.ScreenName,
                MovieName = show.MovieTitle,
                TheatreName = show.TheaterName,
                ShowDate = show.ShowDateTime,
                ShowTime = show.ShowDateTime.ToString("h:mm tt"),
                SelectedSeats = selectedSeatInfos
            };

            return View(viewModel);
        }

        private async Task<(ShowListDto? Show, bool Failed)> FetchShowByIdAsync(int showtimeId, List<string> errors)
        {
            var json = await SafeGetJsonAsync("api/shows", errors);
            if (json == null) return (null, true);

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<ShowListDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return (null, true);

                // Genuinely not in the list = not found, not a failure.
                var match = wrapper.Data.FirstOrDefault(s => s.ShowId == showtimeId);
                return (match, false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/shows");
                errors.Add(GenericLoadError);
                return (null, true);
            }
        }

        private async Task<List<Seat>> FetchSeatsForShowAsync(int showtimeId, List<string> errors)
        {
            var json = await SafeGetJsonAsync($"api/seats/show/{showtimeId}", errors);
            if (json == null) return new List<Seat>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<AvailableSeatDto>>>(json, options);

                if (wrapper?.Success != true || wrapper.Data == null) return new List<Seat>();

                return wrapper.Data.Select(dto => new Seat
                {
                    ShowSeatId = dto.ShowSeatId,
                    SeatId = dto.SeatId,
                    Row = dto.SeatNumber.Length > 0 ? dto.SeatNumber[..1] : "?",
                    Number = int.TryParse(dto.SeatNumber.Length > 1 ? dto.SeatNumber[1..] : "0", out var num) ? num : 0,
                    Tier = Enum.TryParse<PricingTier>(dto.SeatType, true, out var tier) ? tier : PricingTier.Regular,
                    Status = Enum.TryParse<SeatStatus>(dto.Status, true, out var status) ? status : SeatStatus.Available,
                    IsSelected = false
                }).ToList();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Malformed JSON from api/seats/show/{ShowtimeId}", showtimeId);
                errors.Add(GenericLoadError);
                return new List<Seat>();
            }
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            if (!TempData.TryGetValue("BookingConfirmation", out var raw) || raw is not string json)
            {
                return RedirectToAction("Index", "Home");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var viewModel = JsonSerializer.Deserialize<BookingConfirmationViewModel>(json, options);

            if (viewModel == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTicket(int bookingId)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/tickets/{bookingId}/download");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Ticket download returned HTTP {StatusCode} for BookingId {BookingId}.", response.StatusCode, bookingId);
                    TempData["DownloadError"] = "Couldn't download your ticket right now. Please try again shortly.";
                    return RedirectToAction("Confirmation");
                }

                var pdfBytes = await response.Content.ReadAsByteArrayAsync();
                return File(pdfBytes, "application/pdf", $"Ticket_{bookingId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket download failed for BookingId {BookingId}.", bookingId);
                TempData["DownloadError"] = "Couldn't download your ticket right now. Please try again shortly.";
                return RedirectToAction("Confirmation");
            }
        }
    }
}