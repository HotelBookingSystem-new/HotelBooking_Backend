using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Helpers;

namespace Backend.Services
{
    public class PromotionService
    {
        private readonly ApplicationDbContext _context;
        private readonly DiscountCalculator _discountCalculator;
        private readonly ILogger<PromotionService> _logger;

        public PromotionService(
            ApplicationDbContext context,
            DiscountCalculator discountCalculator,
            ILogger<PromotionService> logger)
        {
            _context = context;
            _discountCalculator = discountCalculator;
            _logger = logger;
        }

        public async Task<List<PromotionDto>> GetValidPromotionsAsync()
        {
            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now && p.UsedCount < p.UsageLimit)
                .Select(p => new PromotionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    PromotionType = p.PromotionType,
                    DiscountPercentage = p.DiscountPercentage,
                    MaxDiscountAmount = p.MaxDiscountAmount,
                    MinimumBookingAmount = p.MinimumBookingAmount,
                    ValidFrom = p.ValidFrom,
                    ValidTo = p.ValidTo,
                    UsageLimit = p.UsageLimit,
                    UsedCount = p.UsedCount,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return promotions;
        }

        public async Task<PromotionResultDto> ValidatePromotionAsync(string code, decimal bookingAmount)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);

            if (promotion == null)
            {
                return new PromotionResultDto
                {
                    IsValid = false,
                    Message = "Invalid promotion code"
                };
            }

            var now = DateTime.UtcNow;
            if (promotion.ValidFrom > now || promotion.ValidTo < now)
            {
                return new PromotionResultDto
                {
                    IsValid = false,
                    Message = "Promotion code has expired"
                };
            }

            if (promotion.UsedCount >= promotion.UsageLimit)
            {
                return new PromotionResultDto
                {
                    IsValid = false,
                    Message = "Promotion code usage limit exceeded"
                };
            }

            if (bookingAmount < promotion.MinimumBookingAmount)
            {
                return new PromotionResultDto
                {
                    IsValid = false,
                    Message = $"Minimum booking amount of {promotion.MinimumBookingAmount:C} required"
                };
            }

            var discountAmount = _discountCalculator.CalculateDiscount(bookingAmount, promotion.DiscountPercentage, promotion.MaxDiscountAmount);

            return new PromotionResultDto
            {
                IsValid = true,
                Message = "Promotion code applied successfully",
                Promotion = new PromotionDto
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    Name = promotion.Name,
                    DiscountPercentage = promotion.DiscountPercentage,
                    MaxDiscountAmount = promotion.MaxDiscountAmount
                },
                DiscountAmount = discountAmount,
                FinalAmount = bookingAmount - discountAmount
            };
        }

        public async Task<PromotionResultDto> ApplyPromotionAsync(string code, decimal bookingAmount)
        {
            var validationResult = await ValidatePromotionAsync(code, bookingAmount);

            if (validationResult.IsValid)
            {
                var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
                if (promotion != null)
                {
                    promotion.UsedCount++;
                    await _context.SaveChangesAsync();
                }
            }

            return validationResult;
        }

        public async Task<BulkPromotionResultDto> BulkValidatePromotionsAsync(List<string> codes, decimal bookingAmount)
        {
            var validPromotions = new List<PromotionResultDto>();
            PromotionResultDto bestPromotion = null;
            decimal maxDiscount = 0;

            foreach (var code in codes)
            {
                var result = await ValidatePromotionAsync(code, bookingAmount);
                if (result.IsValid)
                {
                    validPromotions.Add(result);
                    if (result.DiscountAmount > maxDiscount)
                    {
                        maxDiscount = result.DiscountAmount;
                        bestPromotion = result;
                    }
                }
            }

            return new BulkPromotionResultDto
            {
                BestPromotion = bestPromotion,
                AllValidPromotions = validPromotions,
                MaximumPossibleDiscount = maxDiscount
            };
        }

        public async Task<LoyaltyPointsDto> GetLoyaltyPointsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            var nextTier = await _context.Set<LoyaltyReward>()
                .Where(t => t.MinimumPoints > user.LoyaltyPoints)
                .OrderBy(t => t.MinimumPoints)
                .FirstOrDefaultAsync();

            var recentTransactions = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .Select(b => new LoyaltyTransactionDto
                {
                    Points = b.LoyaltyPointsEarned,
                    TransactionType = "Earned",
                    Description = $"Booking #{b.BookingReference}",
                    TransactionDate = b.BookingDate,
                    BookingId = b.Id
                })
                .ToListAsync();

            return new LoyaltyPointsDto
            {
                UserId = userId,
                CurrentPoints = user.LoyaltyPoints,
                CurrentTier = user.LoyaltyTier ?? "Bronze",
                PointsToNextTier = nextTier?.MinimumPoints - user.LoyaltyPoints ?? 0,
                NextTier = nextTier?.TierName,
                RecentTransactions = recentTransactions
            };
        }

        public async Task<List<SeasonalOfferDto>> GetSeasonalOffersAsync()
        {
            var currentSeason = GetCurrentSeason();
            var offers = await _context.Promotions
                .Where(p => p.IsActive && p.PromotionType == "SeasonalOffer" &&
                           p.ValidFrom <= DateTime.UtcNow && p.ValidTo >= DateTime.UtcNow)
                .Select(p => new SeasonalOfferDto
                {
                    Id = p.Id,
                    OfferName = p.Name,
                    Season = currentSeason,
                    StartDate = p.ValidFrom,
                    EndDate = p.ValidTo,
                    DiscountPercentage = p.DiscountPercentage
                })
                .ToListAsync();

            return offers;
        }

        public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto request)
        {
            var promotion = new Promotion
            {
                Code = request.Code.ToUpper(),
                Name = request.Name,
                Description = request.Description,
                PromotionType = request.PromotionType,
                DiscountPercentage = request.DiscountPercentage,
                MaxDiscountAmount = request.MaxDiscountAmount,
                MinimumBookingAmount = request.MinimumBookingAmount,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                UsageLimit = request.UsageLimit,
                UsedCount = 0,
                IsActive = true
            };

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();

            return new PromotionDto
            {
                Id = promotion.Id,
                Code = promotion.Code,
                Name = promotion.Name,
                Description = promotion.Description,
                PromotionType = promotion.PromotionType,
                DiscountPercentage = promotion.DiscountPercentage,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                MinimumBookingAmount = promotion.MinimumBookingAmount,
                ValidFrom = promotion.ValidFrom,
                ValidTo = promotion.ValidTo,
                UsageLimit = promotion.UsageLimit,
                UsedCount = promotion.UsedCount,
                IsActive = promotion.IsActive
            };
        }

        public async Task<PromotionDto> UpdatePromotionAsync(int id, CreatePromotionDto request)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return null;

            promotion.Code = request.Code.ToUpper();
            promotion.Name = request.Name;
            promotion.Description = request.Description;
            promotion.PromotionType = request.PromotionType;
            promotion.DiscountPercentage = request.DiscountPercentage;
            promotion.MaxDiscountAmount = request.MaxDiscountAmount;
            promotion.MinimumBookingAmount = request.MinimumBookingAmount;
            promotion.ValidFrom = request.ValidFrom;
            promotion.ValidTo = request.ValidTo;
            promotion.UsageLimit = request.UsageLimit;

            await _context.SaveChangesAsync();

            return new PromotionDto
            {
                Id = promotion.Id,
                Code = promotion.Code,
                Name = promotion.Name,
                DiscountPercentage = promotion.DiscountPercentage,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                MinimumBookingAmount = promotion.MinimumBookingAmount,
                ValidFrom = promotion.ValidFrom,
                ValidTo = promotion.ValidTo,
                UsageLimit = promotion.UsageLimit,
                UsedCount = promotion.UsedCount,
                IsActive = promotion.IsActive
            };
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
                return false;

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetCurrentSeason()
        {
            var month = DateTime.UtcNow.Month;
            if (month >= 3 && month <= 5) return "Spring";
            if (month >= 6 && month <= 8) return "Summer";
            if (month >= 9 && month <= 11) return "Fall";
            return "Winter";
        }
    }
}