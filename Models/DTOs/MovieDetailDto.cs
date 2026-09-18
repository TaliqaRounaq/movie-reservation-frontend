using CineBook.Models.DTOs;

namespace CineBook.Models.DTOs
{
    public class MovieDetailDto
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<ActorDetailDto> Actors { get; set; } = new();
        public List<CrewDto> Crew { get; set; } = new();
        public DateTime ReleaseDate { get; set; }
        public decimal Rating { get; set; }
        public string Certificate { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public bool IsFeatured { get; set; }
    }
}