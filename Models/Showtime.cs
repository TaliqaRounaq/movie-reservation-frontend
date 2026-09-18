using System;

namespace CineBook.Models
{
    public class Showtime
    {
        public int Id { get; set; }
        public int TheatreId { get; set; }
        public int MovieId { get; set; }
        public Movie Movie { get; set; }
        public string Screen { get; set; }
        public DateTime StartTime { get; set; }
    }
}