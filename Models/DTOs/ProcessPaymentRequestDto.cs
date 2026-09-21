namespace CineBook.Models.DTOs
{
    public class ProcessPaymentRequestDto
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public CineBook.Models.Enums.PaymentMethod PaymentMethod { get; set; }
    }

    public class PaymentDetailDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BookingId { get; set; }
    }
}