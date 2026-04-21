using System;
using System.Collections.Generic;

namespace HotelManagement.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Customer"; // Admin, Customer
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int LoyaltyPoints { get; set; } = 0;
        public string LoyaltyTier { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum

        // Auth-specific (Member A)
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<EmailConfirmation> EmailConfirmations { get; set; } = new List<EmailConfirmation>();
    }
}