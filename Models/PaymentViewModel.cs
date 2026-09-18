namespace CineBook.Models;

public class PaymentViewModel
{
    public int ShowtimeId { get; set; }      // NAYA
    public string ShowSeatIds { get; set; } = string.Empty;  // NAYA — comma-separated

    public int HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string MovieName { get; set; } = string.Empty;
    public string TheatreName { get; set; } = string.Empty;
    public string ShowDate { get; set; } = string.Empty;
    public string ShowTime { get; set; } = string.Empty;
    public string Seats { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
