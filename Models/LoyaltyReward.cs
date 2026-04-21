public class LoyaltyReward
{
    public int Id { get; set; }
    public string TierName { get; set; } // Bronze, Silver, Gold, Platinum
    public int MinimumPoints { get; set; }
    public decimal DiscountRate { get; set; }
    public int PointsPerDollar { get; set; }
    public string Benefits { get; set; }
    public int FreeUpgradeAfterBookings { get; set; }
    public decimal CashbackPercentage { get; set; }
}