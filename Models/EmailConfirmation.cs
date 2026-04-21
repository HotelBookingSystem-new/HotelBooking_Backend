using System;

namespace HotelManagement.Models
{
    public class EmailConfirmation
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string ConfirmationNumber { get; set; } = string.Empty;

        /// <summary>
        /// BookingConfirmation | Cancellation | PasswordReset | Welcome | RebookConfirmation
        /// </summary>
        public string EmailType { get; set; } = "BookingConfirmation";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsDelivered { get; set; } = false;
        public string EmailContent { get; set; } = string.Empty;
        public DateTime? OpenedAt { get; set; }
        public int RetryCount { get; set; } = 0;

        // Navigation
        public Booking? Booking { get; set; }
        public User? User { get; set; }
    }
}