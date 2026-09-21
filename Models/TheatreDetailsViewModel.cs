using System;
using System.Collections.Generic;

namespace CineBook.Models
{
    public class MovieWithShowtimes
    {
        public Movie Movie { get; set; }
        public List<Showtime> Showtimes { get; set; }
    }

    public class TheatreDetailsViewModel
    {
        public Theatre Theatre { get; set; }
        public List<DateTime> AvailableDates { get; set; }
        public DateTime SelectedDate { get; set; }
        public List<MovieWithShowtimes> Movies { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
    }
}