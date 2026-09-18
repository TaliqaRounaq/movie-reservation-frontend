using System.Collections.Generic;

namespace CineBook.Models
{
    public class SeatSelectionViewModel
    {
        public int ShowtimeId { get; set; }
        public int SeatCount { get; set; } = 1;
        public string MovieName { get; set; } = string.Empty;
        public string TheatreName { get; set; } = string.Empty;
        public string ShowDate { get; set; } = string.Empty;
        public string ShowTime { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;

        public List<Seat> Seats { get; set; } = new();
        public decimal Total { get; set; }
    }
}
