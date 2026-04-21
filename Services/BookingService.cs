using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Data;

namespace Backend.Services
{
    public class BookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly PaymentService _paymentService;
        private readonly EmailService _emailService;
        private readonly PromotionService _promotionService;
        private readonly DiscountCalculator _discountCalculator;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            ApplicationDbContext context,
            PaymentService paymentService,
            EmailService emailService,
            PromotionService promotionService,
            DiscountCalculator discountCalculator,
            ILogger<BookingService> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _emailService = emailService;
            _promotionService = promotionService;
            _discountCalculator = discountCalculator;
            _logger = logger;
        }

        public async Task<object> CreateBookingAsync(int userId, BookingRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate dates
                if (!DateValidator.IsValidDateRange(request.CheckInDate, request.CheckOutDate))
                {
                    return new { Success = false, Message = "Invalid date range" };
                }

                // Check availability
                var availability = await CheckAvailabilityAsync(new AvailabilityCheckDto
                {
                    HotelId = await GetHotelIdByRoomId(request.RoomId),
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    RoomCategoryId = null,
                    NumberOfGuests = request.NumberOfGuests
                });

                if (!availability.IsAvailable)
                {
                    return new { Success = false, Message = "Room not available for selected dates" };
                }

                // Get room details
                var room = await _context.Rooms
                    .Include(r => r.RoomCategory)
                    .FirstOrDefaultAsync(r => r.Id == request.RoomId && r.IsAvailable);

                if (room == null)
                {
                    return new { Success = false, Message = "Room not found or unavailable" };
                }

                // Calculate price
                var nights = (request.CheckOutDate - request.CheckInDate).Days;
                var basePrice = room.BasePrice * nights;

                // Apply promotion
                decimal discountAmount = 0;
                Promotion appliedPromotion = null;

                if (!string.IsNullOrEmpty(request.PromotionCode))
                {
                    var promotionResult = await _promotionService.ApplyPromotionAsync(request.PromotionCode, basePrice);
                    if (promotionResult.IsValid)
                    {
                        discountAmount = promotionResult.DiscountAmount;
                        appliedPromotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == request.PromotionCode);
                    }
                }

                // Apply loyalty points
                var user = await _context.Users.FindAsync(userId);
                int loyaltyPointsUsed = 0;
                decimal loyaltyDiscount = 0;

                if (request.LoyaltyPointsToUse > 0 && user.LoyaltyPoints >= request.LoyaltyPointsToUse)
                {
                    loyaltyDiscount = _discountCalculator.CalculateLoyaltyDiscount(request.LoyaltyPointsToUse);
                    loyaltyPointsUsed = request.LoyaltyPointsToUse;
                    discountAmount += loyaltyDiscount;
                    user.LoyaltyPoints -= loyaltyPointsUsed;
                }

                var finalPrice = basePrice - discountAmount;
                var loyaltyPointsEarned = _discountCalculator.CalculateLoyaltyPointsEarned(finalPrice);

                // Create booking
                var booking = new Booking
                {
                    BookingReference = GenerateBookingReference(),
                    UserId = userId,
                    RoomId = request.RoomId,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    NumberOfGuests = request.NumberOfGuests,
                    TotalPrice = basePrice,
                    DiscountAmount = discountAmount,
                    FinalPrice = finalPrice,
                    Status = "Pending",
                    SpecialRequests = request.SpecialRequests,
                    BookingDate = DateTime.UtcNow,
                    PromotionId = appliedPromotion?.Id,
                    LoyaltyPointsUsed = loyaltyPointsUsed,
                    LoyaltyPointsEarned = loyaltyPointsEarned
                };

                await _context.Bookings.AddAsync(booking);
                await _context.SaveChangesAsync();

                // Process payment
                var paymentResult = await _paymentService.ProcessPaymentAsync(booking.Id, request.PaymentDetails, finalPrice);

                if (!paymentResult.Success)
                {
                    await transaction.RollbackAsync();
                    return new { Success = false, Message = "Payment failed: " + paymentResult.Message };
                }

                // Update booking status
                booking.Status = "Confirmed";
                booking.Payments = new List<Payment> { new Payment
                {
                    Amount = finalPrice,
                    PaymentMethod = request.PaymentDetails.PaymentMethod,
                    TransactionId = paymentResult.TransactionId,
                    PaymentStatus = "Success",
                    PaymentDate = DateTime.UtcNow
                }};

                // Update user loyalty points
                user.LoyaltyPoints += loyaltyPointsEarned;
                await UpdateLoyaltyTier(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send email confirmation
                await _emailService.SendBookingConfirmationAsync(booking.Id, user.Email);

                // Update room availability (you might want to mark as booked for these dates)
                // This would require a more sophisticated availability tracking system

                return new
                {
                    Success = true,
                    Message = "Booking created successfully",
                    BookingId = booking.Id,
                    BookingReference = booking.BookingReference,
                    FinalPrice = finalPrice,
                    LoyaltyPointsEarned = loyaltyPointsEarned
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating booking");
                throw;
            }
        }

        public async Task<List<BookingHistoryDto>> GetUserBookingsAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .Include(b => b.Room.RoomCategory)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new BookingHistoryDto
                {
                    Id = b.Id,
                    BookingReference = b.BookingReference,
                    HotelName = b.Room.Hotel.Name,
                    RoomCategory = b.Room.RoomCategory.Name,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    FinalPrice = b.FinalPrice,
                    Status = b.Status,
                    BookingDate = b.BookingDate,
                    CanCancel = b.Status == "Confirmed" && b.CheckInDate > DateTime.UtcNow.AddDays(1),
                    CanRebook = b.Status == "Cancelled" || (b.CheckOutDate < DateTime.UtcNow && b.Status == "Confirmed")
                })
                .ToListAsync();

            return bookings;
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId, int userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Hotel)
                .Include(b => b.Room.RoomCategory)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return null;

            return new BookingResponseDto
            {
                Id = booking.Id,
                BookingReference = booking.BookingReference,
                UserId = booking.UserId,
                RoomId = booking.RoomId,
                HotelName = booking.Room.Hotel.Name,
                RoomNumber = booking.Room.RoomNumber,
                RoomCategory = booking.Room.RoomCategory.Name,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfGuests = booking.NumberOfGuests,
                TotalPrice = booking.TotalPrice,
                DiscountAmount = booking.DiscountAmount,
                FinalPrice = booking.FinalPrice,
                LoyaltyPointsEarned = booking.LoyaltyPointsEarned,
                LoyaltyPointsUsed = booking.LoyaltyPointsUsed,
                Status = booking.Status,
                BookingDate = booking.BookingDate,
                SpecialRequests = booking.SpecialRequests,
                Payment = booking.Payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    TransactionId = p.TransactionId,
                    PaymentStatus = p.PaymentStatus,
                    PaymentDate = p.PaymentDate,
                    CardLastFourDigits = p.CardLastFourDigits
                }).FirstOrDefault()
            };
        }

        public async Task<object> CancelBookingAsync(int bookingId, int userId, string reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return new { Success = false, Message = "Booking not found" };

            if (booking.Status != "Confirmed")
                return new { Success = false, Message = "Booking cannot be cancelled" };

            if (booking.CheckInDate <= DateTime.UtcNow.AddDays(1))
                return new { Success = false, Message = "Bookings can only be cancelled at least 24 hours before check-in" };

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = reason;

            // Refund logic would go here
            // Return loyalty points if applicable

            await _context.SaveChangesAsync();

            // Send cancellation email
            await _emailService.SendBookingCancellationAsync(booking.User.Email, booking.BookingReference);

            return new { Success = true, Message = "Booking cancelled successfully" };
        }

        public async Task<object> RebookAsync(int userId, RebookDto request)
        {
            var originalBooking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == request.OriginalBookingId && b.UserId == userId);

            if (originalBooking == null)
                return new { Success = false, Message = "Original booking not found" };

            var newBookingRequest = new BookingRequestDto
            {
                RoomId = originalBooking.RoomId,
                CheckInDate = request.NewCheckInDate ?? originalBooking.CheckInDate,
                CheckOutDate = request.NewCheckOutDate ?? originalBooking.CheckOutDate,
                NumberOfGuests = request.NumberOfGuests ?? originalBooking.NumberOfGuests,
                SpecialRequests = originalBooking.SpecialRequests
            };

            return await CreateBookingAsync(userId, newBookingRequest);
        }

        public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(AvailabilityCheckDto request)
        {
            var nights = (request.CheckOutDate - request.CheckInDate).Days;

            var availableRooms = await _context.Rooms
                .Include(r => r.RoomCategory)
                .Include(r => r.Hotel)
                .Where(r => r.HotelId == request.HotelId && r.IsAvailable && r.Capacity >= request.NumberOfGuests)
                .Where(r => !_context.Bookings.Any(b => b.RoomId == r.Id &&
                    b.Status == "Confirmed" &&
                    ((b.CheckInDate <= request.CheckInDate && b.CheckOutDate > request.CheckInDate) ||
                     (b.CheckInDate < request.CheckOutDate && b.CheckOutDate >= request.CheckOutDate) ||
                     (b.CheckInDate >= request.CheckInDate && b.CheckOutDate <= request.CheckOutDate))))
                .Select(r => new AvailableRoomDto
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    RoomCategory = r.RoomCategory.Name,
                    Capacity = r.Capacity,
                    PricePerNight = r.BasePrice,
                    TotalPrice = r.BasePrice * nights
                })
                .ToListAsync();

            return new AvailabilityResponseDto
            {
                IsAvailable = availableRooms.Any(),
                AvailableRoomsCount = availableRooms.Count,
                AvailableRooms = availableRooms,
                TotalPrice = availableRooms.Any() ? availableRooms.Min(r => r.TotalPrice) : 0,
                Nights = nights
            };
        }

        public async Task<BookingSummaryDto> GetBookingSummaryAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            return new BookingSummaryDto
            {
                TotalBookings = bookings.Count,
                ActiveBookings = bookings.Count(b => b.Status == "Confirmed" && b.CheckOutDate > DateTime.UtcNow),
                CompletedBookings = bookings.Count(b => b.Status == "Confirmed" && b.CheckOutDate <= DateTime.UtcNow),
                CancelledBookings = bookings.Count(b => b.Status == "Cancelled"),
                TotalSpent = bookings.Where(b => b.Status == "Confirmed").Sum(b => b.FinalPrice),
                TotalLoyaltyPointsEarned = bookings.Sum(b => b.LoyaltyPointsEarned),
                UpcomingBookings = bookings
                    .Where(b => b.Status == "Confirmed" && b.CheckInDate > DateTime.UtcNow)
                    .OrderBy(b => b.CheckInDate)
                    .Take(5)
                    .Select(b => new UpcomingBookingDto
                    {
                        BookingId = b.Id,
                        BookingReference = b.BookingReference,
                        HotelName = _context.Rooms.Include(r => r.Hotel).FirstOrDefault(r => r.Id == b.RoomId).Hotel.Name,
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        DaysUntilCheckIn = (b.CheckInDate - DateTime.UtcNow).Days
                    }).ToList()
            };
        }

        public async Task<object> ResendConfirmationEmailAsync(int bookingId, int userId)
        {
            var booking = await GetBookingByIdAsync(bookingId, userId);
            if (booking == null)
                return new { Success = false, Message = "Booking not found" };

            var user = await _context.Users.FindAsync(userId);
            await _emailService.SendBookingConfirmationAsync(bookingId, user.Email);

            return new { Success = true, Message = "Confirmation email sent successfully" };
        }

        private string GenerateBookingReference()
        {
            return "BK" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private async Task<int> GetHotelIdByRoomId(int roomId)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
            return room?.HotelId ?? 0;
        }

        private async Task UpdateLoyaltyTier(User user)
        {
            var tier = await _context.Set<LoyaltyReward>()
                .Where(t => t.MinimumPoints <= user.LoyaltyPoints)
                .OrderByDescending(t => t.MinimumPoints)
                .FirstOrDefaultAsync();

            if (tier != null)
            {
                user.LoyaltyTier = tier.TierName;
            }
        }
    }
}