public class BookingConfirmationViewModel
{
    public string ConfirmationId { get; set; } = string.Empty;
    public int BookingId { get; set; }              // NEW — download link ke liye chahiye
    public string MovieName { get; set; } = string.Empty;
    public string TheatreName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public string ShowDate { get; set; } = string.Empty;
    public string ShowTime { get; set; } = string.Empty;
    public List<string> Seats { get; set; } = new();
    public decimal AmountPaid { get; set; }
    public string? TicketNumber { get; set; }        // NEW — null agar fetch fail hui
}