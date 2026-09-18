using CineBook.Models;
using Microsoft.AspNetCore.Authorization;
using CineBook.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CineBook.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly HttpClient _httpClient;

        public BookingController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("BackendApi");
        }

        [HttpGet]
        public async Task<IActionResult> Summary(int showtimeId, string showSeatIds)
        {
            if (string.IsNullOrEmpty(showSeatIds))
            {
                return RedirectToAction("Index", "Seats", new { showtimeId });
            }

            List<int> selectedIds = showSeatIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

            ShowListDto? show = await FetchShowByIdAsync(showtimeId);
            if (show == null) return NotFound();

            List<Seat> allSeats = await FetchSeatsForShowAsync(showtimeId);
            List<Seat> selectedSeats = allSeats.Where(s => selectedIds.Contains(s.ShowSeatId)).ToList();

            if (selectedSeats.Count == 0) return BadRequest("No valid seats selected.");

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
                Tier = Enum.TryParse<PricingTier>(dto.SeatType, true, out var tier) ? tier : PricingTier.Regular,
                Status = Enum.TryParse<SeatStatus>(dto.Status, true, out var status) ? status : SeatStatus.Available,
                IsSelected = false
            }).ToList();
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
    }
}
