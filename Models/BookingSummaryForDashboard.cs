using System;

namespace CineBook.Models
{
    public class BookingSummaryForDashboard
    {
        public string MovieTitle { get; set; } = "";
        public string TheatreName { get; set; } = "";
        public DateTime ShowDateTime { get; set; }
        public string Seats { get; set; } = "";
    }
}