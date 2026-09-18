namespace CineBook.Models
{
    public class Seat
    {
        public int ShowSeatId { get; set; }   // reserve/release ke liye chahiye
        public int SeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
        public PricingTier Tier { get; set; }
        public SeatStatus Status { get; set; }
        public bool IsSelected { get; set; }   // client-side: is user ne abhi select kiya hai?
    }
}