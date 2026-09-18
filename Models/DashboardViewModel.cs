using CineBook.Models;
public class DashboardViewModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<BookingSummaryForDashboard> UpcomingBookings { get; set; } = new();
    public List<Offer> Offers { get; set; } = new();
    public List<BookingSummaryForDashboard> PastBookings { get; set; } = new();
    public List<PaymentMethod> PaymentMethods { get; set; } = new();
}