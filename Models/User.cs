public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Role { get; set; } // Admin, Customer
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public int LoyaltyPoints { get; set; }
    public string LoyaltyTier { get; set; } // Bronze, Silver, Gold, Platinum
    public ICollection<Booking> Bookings { get; set; }
}