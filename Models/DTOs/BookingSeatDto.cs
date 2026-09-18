namespace CineBook.Models.DTOs
{
    public class BookingSeatDto
    {
        public int ShowSeatId { get; set; }
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
    }
}