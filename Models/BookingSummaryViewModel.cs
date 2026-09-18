using System;
using System.Collections.Generic;
using System.Linq;

namespace CineBook.Models
{
    public class SelectedSeatInfo
    {
        public int ShowSeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
        public PricingTier Tier { get; set; }
        public decimal Price { get; set; }

        public string SeatLabel => $"{Row}{Number}";
    }

    public class BookingSummaryViewModel
    {
        public int ShowtimeId { get; set; }
        public string ScreenName { get; set; } = string.Empty;

        public string MovieName { get; set; } = string.Empty;
        public string MoviePosterUrl { get; set; } = string.Empty;
        public DateTime ShowDate { get; set; }
        public string ShowTime { get; set; } = string.Empty;
        public string TheatreName { get; set; } = string.Empty;

        public List<SelectedSeatInfo> SelectedSeats { get; set; } = new List<SelectedSeatInfo>();

        public decimal Subtotal => SelectedSeats.Sum(s => s.Price);
        public decimal BookingFee { get; set; } = 1.00m;
        public decimal GST => Math.Round(Subtotal * 0.05m, 0);
        public decimal Total => Subtotal + BookingFee + GST;
    }
}