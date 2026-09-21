namespace CineBook.Models.DTOs
{
	public class TicketDetailDto
	{
		public int TicketId { get; set; }
		public string TicketNumber { get; set; } = string.Empty;
		public DateTime GeneratedAt { get; set; }
		public string QRCodePath { get; set; } = string.Empty;
		public int BookingId { get; set; }
		public string MovieTitle { get; set; } = string.Empty;
		public string TheaterName { get; set; } = string.Empty;
		public string ScreenName { get; set; } = string.Empty;
		public string ShowDate { get; set; } = string.Empty;
		public string ShowTime { get; set; } = string.Empty;
		public string Seats { get; set; } = string.Empty;
		public decimal Amount { get; set; }
	}
}