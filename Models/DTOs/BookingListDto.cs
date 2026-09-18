using System;

namespace CineBook.Models.DTOs
{
    public class BookingListDto
    {
        public int BookingId { get; set; }
        public int ShowId { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}