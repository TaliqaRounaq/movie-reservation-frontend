namespace CineBook.Models.DTOs
{
    public class ShowListDto
    {
        public int ShowId { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int TheaterId { get; set; }
        public string TheaterName { get; set; } = string.Empty;
        public int ScreenId { get; set; }
        public string ScreenName { get; set; } = string.Empty;
        public DateTime ShowDateTime { get; set; }
    }
}