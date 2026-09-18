namespace CineBook.Models
{
    public class MovieDetailViewModel
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public string Certificate { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double? Rating { get; set; }

        public string? PosterUrl { get; set; }

        public string AboutText { get; set; } = string.Empty;
        public List<string> Cast { get; set; } = new();
        public List<string> Crew { get; set; } = new();

        public List<TheatreAvailability> Availability { get; set; } = new();
    }

    public class TheatreAvailability
    {
        public string TheatreName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public List<ShowtimeSlot> Times { get; set; } = new();
    }
    public class ShowtimeSlot
    {
        public int ShowId { get; set; }
        public string Time { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
    }
}
