public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } // CreditCard, DebitCard, PayPal, UPI
    public string TransactionId { get; set; }
    public string PaymentStatus { get; set; } // Pending, Success, Failed, Refunded
    public DateTime PaymentDate { get; set; }
    public string CardLastFourDigits { get; set; }
    public string BillingAddress { get; set; }
    public Booking Booking { get; set; }
}