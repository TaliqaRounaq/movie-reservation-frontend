namespace CineBook.Models.DTOs
{
    public class AvailableSeatDto
    {
        public int ShowSeatId { get; set; }
        public int SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}