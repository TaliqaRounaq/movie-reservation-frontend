using CineBook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CineBook.Models.DTOs;
using System.Text.Json;

namespace CineBook.Controllers;

public class PaymentController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IHttpClientFactory httpClientFactory, ILogger<PaymentController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BackendApi");
        _logger = logger;
    }
    [HttpGet]
    public IActionResult Index(PaymentViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Seats) || model.Amount <= 0) return RedirectToAction("Index", "Home");
        return View(model);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(PaymentViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Seats) || model.Amount <= 0)
            return RedirectToAction("Index", "Home");

        var token = HttpContext.Session.GetString("JwtToken");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Account");
        if (!TryGetShowSeatIds(model.ShowSeatIds, out var showSeatIds))
        {
            TempData["BookingError"] = "Your selected seats are invalid. Please choose your seats again.";
            return RedirectToAction("Index", "Seats", new { showtimeId = model.ShowtimeId });
        }

        try
        {
            // A seat hold lasts five minutes. Renew it immediately before booking.
            var reservationError = await RenewSeatReservationsAsync(showSeatIds, token);
            if (reservationError != null)
            {
                TempData["BookingError"] = reservationError;
                return RedirectToPayment(model);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/bookings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new CreateBookingDto
            {
                ShowId = model.ShowtimeId,
                ShowSeatIds = showSeatIds
            });

            using var response = await _httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("JwtToken");
                return RedirectToAction("Login", "Account");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Booking API returned HTTP {StatusCode} for show {ShowtimeId}.", response.StatusCode, model.ShowtimeId);
                TempData["BookingError"] = "We could not process your booking right now. Please try again shortly.";
                return RedirectToPayment(model);
            }

            var json = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<BookingDetailDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (wrapper?.Success != true || wrapper.Data == null)
            {
                _logger.LogWarning("Booking API rejected booking for show {ShowtimeId}: {Message}", model.ShowtimeId, wrapper?.Message);
                TempData["BookingError"] = string.IsNullOrWhiteSpace(wrapper?.Message)
                    ? "We could not complete your booking. Please try again."
                    : wrapper.Message;
                return RedirectToPayment(model);
            }

        var booking = wrapper.Data;
        var confirmation = new BookingConfirmationViewModel
        {
            ConfirmationId = "CB" + booking.BookingId.ToString("D6"),
            MovieName = string.IsNullOrWhiteSpace(booking.MovieTitle) ? model.MovieName : booking.MovieTitle,
            TheatreName = string.IsNullOrWhiteSpace(booking.TheaterName) ? model.TheatreName : booking.TheaterName,
            HallName = string.IsNullOrWhiteSpace(booking.ScreenName) ? model.HallName : booking.ScreenName,
            ShowDate = booking.ShowDateTime == default
                ? FormatShowDate(model.ShowDate)
                : booking.ShowDateTime.ToString("dd MMM yyyy"),
            ShowTime = booking.ShowDateTime == default
                ? model.ShowTime
                : booking.ShowDateTime.ToString("h:mm tt"),
            Seats = booking.Seats.Count > 0
                ? booking.Seats.Select(s => s.SeatNumber).ToList()
                : model.Seats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            // The current booking API returns a fixed TotalAmount for every booking.
            // Use the frontend checkout total, which is based on the selected seats.
            AmountPaid = model.Amount
        };

            TempData["BookingConfirmation"] = JsonSerializer.Serialize(confirmation);
            HttpContext.Session.Remove($"ReservedSeats:{model.ShowtimeId}");

            return RedirectToAction("Confirmation", "Booking");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Booking API could not be reached for show {ShowtimeId}.", model.ShowtimeId);
            TempData["BookingError"] = "The booking service is temporarily unavailable. Please try again shortly.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Booking API timed out for show {ShowtimeId}.", model.ShowtimeId);
            TempData["BookingError"] = "The booking request timed out. Your seats are still reserved; please try again.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Booking API returned an invalid response for show {ShowtimeId}.", model.ShowtimeId);
            TempData["BookingError"] = "The booking service returned an unexpected response. Please try again shortly.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected checkout error for show {ShowtimeId}.", model.ShowtimeId);
            TempData["BookingError"] = "An unexpected error occurred while completing your booking. Please try again.";
        }

        return RedirectToPayment(model);
    }

    private static string FormatShowDate(string value) =>
        DateTime.TryParse(value, out var date) ? date.ToString("dd MMM yyyy") : value;

    private IActionResult RedirectToPayment(PaymentViewModel model) =>
        RedirectToAction("Index", new { model.ShowtimeId, model.ShowSeatIds, model.Seats, model.MovieName, model.TheatreName, model.ShowDate, model.ShowTime, model.Amount });

    private async Task<string?> RenewSeatReservationsAsync(IEnumerable<int> showSeatIds, string token)
    {
        foreach (var showSeatId in showSeatIds)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/seats/reserve");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { showSeatId });

            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponseWrapper<bool>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!response.IsSuccessStatusCode || result?.Success != true)
            {
                var message = string.IsNullOrWhiteSpace(result?.Message)
                    ? "One or more selected seats are no longer available. Please choose your seats again."
                    : result.Message;
                return $"Unable to continue with payment: {message}";
            }
        }

        return null;
    }

    private static bool TryGetShowSeatIds(string value, out List<int> showSeatIds)
    {
        showSeatIds = new List<int>();
        if (string.IsNullOrWhiteSpace(value)) return false;

        var uniqueSeatIds = new HashSet<int>();

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var showSeatId) || showSeatId <= 0 || !uniqueSeatIds.Add(showSeatId))
                return false;

            showSeatIds.Add(showSeatId);
        }

        return showSeatIds.Count > 0;
    }
    //[HttpGet]
    //public IActionResult Confirmation()
    //{
    //    if (!TempData.TryGetValue("BookingConfirmation", out var raw) || raw is not string json)
    //    {
    //        return RedirectToAction("Index", "Home");
    //    }

    //    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    //    var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<BookingDetailDto>>(json, options);

    //    if (wrapper?.Success != true || wrapper.Data == null)
    //    {
    //        return RedirectToAction("Index", "Home");
    //    }

    //    var booking = wrapper.Data;

    //    var viewModel = new BookingConfirmationViewModel
    //    {
    //        ConfirmationId = "CB" + booking.BookingId.ToString("D6"),
    //        MovieName = booking.MovieTitle,
    //        TheatreName = booking.TheaterName,
    //        HallName = booking.ScreenName,
    //        ShowDate = booking.ShowDateTime.ToString("dd MMM yyyy"),
    //        ShowTime = booking.ShowDateTime.ToString("h:mm tt"),
    //        Seats = booking.Seats.Select(s => s.SeatNumber).ToList(),
    //        AmountPaid = booking.TotalAmount
    //    };

    //    return View(viewModel);
    //}
}
