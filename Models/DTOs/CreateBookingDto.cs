namespace CineBook.Models.DTOs
{
    public class CreateBookingDto
    {
        public int ShowId { get; set; }
        public List<int> ShowSeatIds { get; set; } = new();
    }
}