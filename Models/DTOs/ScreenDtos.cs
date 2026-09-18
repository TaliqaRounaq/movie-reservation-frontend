namespace CineBook.Models.DTOs
{
    public class ScreenListDto
    {
        public int ScreenId { get; set; }
        public string ScreenName { get; set; } = string.Empty;
        public int TheaterId { get; set; }
    }

    public class ScreenShowDto
    {
        public int ShowId { get; set; }
        public int MovieId { get; set; }
        public DateTime ShowTime { get; set; }
    }
}