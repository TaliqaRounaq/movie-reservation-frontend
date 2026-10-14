namespace CineBook.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public string Certificate { get; set; } = string.Empty;
        public double? Rating { get; set; }
        public string? ReleaseNote { get; set; }
        public string? PosterUrl { get; set; }
        public bool IsFeatured { get; set; }
    }
    public class Theatre
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Area { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class HomeViewModel
    {
        public List<Movie> HeroMovies { get; set; } = new();
        public List<Movie> NowShowing { get; set; } = new();
        public List<Movie> Recommended { get; set; } = new();
        public List<Movie> Upcoming { get; set; } = new();
        public List<Theatre> NearbyTheatres { get; set; } = new();

        public List<string> Genres { get; set; } = new();
        public List<string> Actors { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();

    }

    public class MovieSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<Movie> Movies { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public List<string> Languages { get; set; } = new();
        public List<Theatre> Theatres { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
    }
}
