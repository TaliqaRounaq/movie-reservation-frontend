namespace CineBook.Models.DTOs
{
    public class TheaterListDto
    {
        public int TheaterId { get; set; }
        public string TheaterName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}