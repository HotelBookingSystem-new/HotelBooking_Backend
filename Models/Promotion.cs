public class Promotion
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PromotionType { get; set; } // DiscountCode, SeasonalOffer, EarlyBird
    public decimal DiscountPercentage { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public decimal MinimumBookingAmount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Booking> Bookings { get; set; }
}