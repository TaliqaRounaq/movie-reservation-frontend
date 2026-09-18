namespace CineBook.Models;

public class SeatCountViewModel
{
    public int ShowtimeId { get; set; }
    public string MovieName { get; set; } = string.Empty;
    public string TheatreName { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty;
    public string ShowDate { get; set; } = string.Empty;
    public string ShowTime { get; set; } = string.Empty;
    public int SeatCount { get; set; } = 1;
}
