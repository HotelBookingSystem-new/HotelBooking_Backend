public class Booking
{
    public int Id { get; set; }
    public string BookingReference { get; set; } // Unique reservation number
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public string Status { get; set; } // Pending, Confirmed, CheckedIn, CheckedOut, Cancelled, NoShow
    public string SpecialRequests { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string CancellationReason { get; set; }
    public int? PromotionId { get; set; }
    public int LoyaltyPointsUsed { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public User User { get; set; }
    public Room Room { get; set; }
    public Promotion Promotion { get; set; }
    public ICollection<Payment> Payments { get; set; }
}