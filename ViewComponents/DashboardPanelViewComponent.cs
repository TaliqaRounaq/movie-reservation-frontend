using Microsoft.AspNetCore.Mvc;
using CineBook.Models;
using CineBook.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CineBook.Controllers
{
    public class DashboardPanelViewComponent : ViewComponent
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DashboardPanelViewComponent> _logger;

        public DashboardPanelViewComponent(IHttpClientFactory httpClientFactory, ILogger<DashboardPanelViewComponent> logger)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "";
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? "";
            var token = HttpContext.Session.GetString("JwtToken");

            var model = new DashboardViewModel
            {
                Name = userName,
                Email = userEmail,
                Offers = new(),
                PaymentMethods = new()
            };

            if (!string.IsNullOrEmpty(token))
            {
                List<BookingDetailDto> allBookings;
                try
                {
                    allBookings = await FetchAllBookingDetailsAsync(token);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Dashboard bookings are unavailable.");
                    allBookings = new();
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "Dashboard bookings request timed out.");
                    allBookings = new();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Dashboard bookings response was invalid.");
                    allBookings = new();
                }

                model.UpcomingBookings = allBookings
                    .Where(b => b.ShowDateTime >= DateTime.Now)
                    .OrderBy(b => b.ShowDateTime)
                    .Select(MapToSummary)
                    .ToList();

                model.PastBookings = allBookings
                    .Where(b => b.ShowDateTime < DateTime.Now)
                    .OrderByDescending(b => b.ShowDateTime)
                    .Select(MapToSummary)
                    .ToList();
            }

            return View("Default", model);
        }

        private async Task<List<BookingDetailDto>> FetchAllBookingDetailsAsync(string token)
        {
            var summaries = await FetchMyBookingsAsync(token);
            var details = new List<BookingDetailDto>();

            foreach (var summary in summaries)
            {
                var detail = await FetchBookingDetailAsync(summary.BookingId, token);
                if (detail != null)
                {
                    details.Add(detail);
                }
            }

            return details;
        }

        private async Task<List<BookingListDto>> FetchMyBookingsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/bookings/my-bookings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<BookingListDto>();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<List<BookingListDto>>>(json, options);

            return wrapper?.Success == true && wrapper.Data != null
                ? wrapper.Data
                : new List<BookingListDto>();
        }

        private async Task<BookingDetailDto?> FetchBookingDetailAsync(int bookingId, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/bookings/{bookingId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = JsonSerializer.Deserialize<ApiResponseWrapper<BookingDetailDto>>(json, options);

            return wrapper?.Success == true ? wrapper.Data : null;
        }

        private static BookingSummaryForDashboard MapToSummary(BookingDetailDto detail)
        {
            return new BookingSummaryForDashboard
            {
                MovieTitle = detail.MovieTitle,
                TheatreName = detail.TheaterName,
                ShowDateTime = detail.ShowDateTime,
                Seats = string.Join(", ", detail.Seats.Select(s => s.SeatNumber))
            };
        }
    }
}
