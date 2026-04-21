using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.DTOs
{
    public class PromotionDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PromotionType { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public decimal MinimumBookingAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsValid { get; }
    }

    public class CreatePromotionDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public string PromotionType { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPercentage { get; set; }

        [Range(0, 100000)]
        public decimal MaxDiscountAmount { get; set; }

        [Range(0, 100000)]
        public decimal MinimumBookingAmount { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }

        [Range(1, 10000)]
        public int UsageLimit { get; set; }
    }

    public class ApplyPromotionDto
    {
        [Required]
        public string PromotionCode { get; set; }

        [Required]
        public decimal BookingAmount { get; set; }

        public int UserId { get; set; }
    }

    public class PromotionResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public PromotionDto Promotion { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class LoyaltyRewardDto
    {
        public int Id { get; set; }
        public string TierName { get; set; }
        public int MinimumPoints { get; set; }
        public decimal DiscountRate { get; set; }
        public int PointsPerDollar { get; set; }
        public string Benefits { get; set; }
        public int FreeUpgradeAfterBookings { get; set; }
        public decimal CashbackPercentage { get; set; }
    }

    public class LoyaltyPointsDto
    {
        public int UserId { get; set; }
        public int CurrentPoints { get; set; }
        public string CurrentTier { get; set; }
        public int PointsToNextTier { get; set; }
        public string NextTier { get; set; }
        public List<LoyaltyTransactionDto> RecentTransactions { get; set; }
    }

    public class LoyaltyTransactionDto
    {
        public int Points { get; set; }
        public string TransactionType { get; set; } // Earned, Used, Expired
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? BookingId { get; set; }
    }

    public class SeasonalOfferDto
    {
        public int Id { get; set; }
        public string OfferName { get; set; }
        public string Season { get; set; } // Summer, Winter, Spring, Fall, Holiday
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DiscountPercentage { get; set; }
        public List<string> ApplicableCities { get; set; }
        public List<int> ApplicableHotelIds { get; set; }
    }

    public class ValidatePromotionDto
    {
        [Required]
        public string PromotionCode { get; set; }

        [Required]
        public decimal BookingAmount { get; set; }

        public DateTime CheckInDate { get; set; }

        public int? UserLoyaltyPoints { get; set; }
    }

    public class BulkPromotionDto
    {
        [Required]
        public List<string> PromotionCodes { get; set; }

        [Required]
        public decimal BookingAmount { get; set; }

        public int UserId { get; set; }
    }

    public class BulkPromotionResultDto
    {
        public PromotionResultDto BestPromotion { get; set; }
        public List<PromotionResultDto> AllValidPromotions { get; set; }
        public decimal MaximumPossibleDiscount { get; set; }
    }
}