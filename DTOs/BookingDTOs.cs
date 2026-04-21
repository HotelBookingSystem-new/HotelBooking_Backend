using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.DTOs
{
    public class BookingRequestDto
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 10)]
        public int NumberOfGuests { get; set; }

        public string SpecialRequests { get; set; }

        public string PromotionCode { get; set; }

        public int LoyaltyPointsToUse { get; set; }

        public PaymentDetailsDto PaymentDetails { get; set; }
    }

    public class PaymentDetailsDto
    {
        [Required]
        public string PaymentMethod { get; set; }

        public string CardNumber { get; set; }

        public string Email { get; set; }

        public string CardHolderName { get; set; }

        public string ExpiryDate { get; set; }

        public string CVV { get; set; }

        public string BillingAddress { get; set; }

        public string UpiId { get; set; }
    }

    public class BookingResponseDto
    {
        public int Id { get; set; }
        public string BookingReference { get; set; }
        public int UserId { get; set; }
        public int RoomId { get; set; }
        public string HotelName { get; set; }
        public string RoomNumber { get; set; }
        public string RoomCategory { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public int LoyaltyPointsEarned { get; set; }
        public int LoyaltyPointsUsed { get; set; }
        public string Status { get; set; }
        public DateTime BookingDate { get; set; }
        public string SpecialRequests { get; set; }
        public string PromotionCode { get; set; }
        public PaymentDto Payment { get; set; }
    }

    public class PaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CardLastFourDigits { get; set; }
    }

    public class BookingHistoryDto
    {
        public int Id { get; set; }
        public string BookingReference { get; set; }
        public string HotelName { get; set; }
        public string RoomCategory { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal FinalPrice { get; set; }
        public string Status { get; set; }
        public DateTime BookingDate { get; set; }
        public bool CanCancel { get; set; }
        public bool CanRebook { get; set; }
    }

    public class CancelBookingDto
    {
        [Required]
        public int BookingId { get; set; }

        public string CancellationReason { get; set; }
    }

    public class BookingSummaryDto
    {
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalLoyaltyPointsEarned { get; set; }
        public List<UpcomingBookingDto> UpcomingBookings { get; set; }
    }

    public class UpcomingBookingDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string HotelName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int DaysUntilCheckIn { get; set; }
    }

    public class RebookDto
    {
        [Required]
        public int OriginalBookingId { get; set; }

        public DateTime? NewCheckInDate { get; set; }
        public DateTime? NewCheckOutDate { get; set; }
        public int? NumberOfGuests { get; set; }
    }

    public class AvailabilityCheckDto
    {
        [Required]
        public int HotelId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public int? RoomCategoryId { get; set; }
        public int NumberOfGuests { get; set; }
    }

    public class AvailabilityResponseDto
    {
        public bool IsAvailable { get; set; }
        public int AvailableRoomsCount { get; set; }
        public List<AvailableRoomDto> AvailableRooms { get; set; }
        public decimal TotalPrice { get; set; }
        public int Nights { get; set; }
    }

    public class AvailableRoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public string RoomCategory { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal TotalPrice { get; set; }
        public List<string> Amenities { get; set; }
    }
    
}