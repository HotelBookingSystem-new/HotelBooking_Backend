using System;

namespace Backend.DTOs
{
    public class EmailRequestDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public EmailType Type { get; set; }
        public dynamic Data { get; set; } = null!;
    }

    public enum EmailType
    {
        BookingConfirmation,
        BookingCancellation,
        RegistrationWelcome,
        RebookingConfirmation,
        PaymentConfirmation,
        ResendConfirmation
    }

    public class BookingEmailData
    {
        public string BookingReference { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomCategory { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public int LoyaltyPointsEarned { get; set; }
        public string SpecialRequests { get; set; } = string.Empty;
    }

    public class CancellationEmailData
    {
        public string BookingReference { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
    }

    public class RegistrationEmailData
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public string LoyaltyTier { get; set; } = string.Empty;
    }

    public class RebookingEmailData
    {
        public string OldBookingReference { get; set; } = string.Empty;
        public string NewBookingReference { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal NewPrice { get; set; }
    }

    public class PaymentEmailData
    {
        public string BookingReference { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    public class ResendConfirmationData
    {
        public string BookingReference { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
    }
}