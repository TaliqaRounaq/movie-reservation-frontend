namespace CineBook.Models.DTOs
{
    public class BookingDetailDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int ShowId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string TheaterName { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
        public DateTime ShowDateTime { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<BookingSeatDto> Seats { get; set; } = new();
    }
}