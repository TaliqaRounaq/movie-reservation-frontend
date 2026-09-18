namespace CineBook.Models.DTOs
{
    public class MovieListDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int Duration { get; set; }
        public string? PosterUrl { get; set; }
        public bool IsFeatured { get; set; }
    }
}