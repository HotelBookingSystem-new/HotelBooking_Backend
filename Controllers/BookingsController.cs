using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using System.Reflection.Metadata.Ecma335;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly BookingService _bookingService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(BookingService bookingService, ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequestDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bookingService.CreateBookingAsync(userId, request);

                if (result.Success)
                    return Ok(result);

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, new { Success = false, Message = "An error occurred while creating booking" });
            }
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var bookings = await _bookingService.GetUserBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user bookings");
                return StatusCode(500, new { Success = false, Message = "An error occurred while fetching bookings" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var booking = await _bookingService.GetBookingByIdAsync(id, userId);

                if (booking == null)
                    return NotFound(new { Success = false, Message = "Booking not found" });

                return Ok(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking by id");
                return StatusCode(500, new { Success = false, Message = "An error occurred while fetching booking" });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bookingService.CancelBookingAsync(request.BookingId, userId, request.CancellationReason);

               

                return result.Success?  Ok(result): BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking");
                return StatusCode(500, new { Success = false, Message = "An error occurred while cancelling booking" });
            }
        }

        [HttpPost("rebook")]
        public async Task<IActionResult> Rebook([FromBody] RebookDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bookingService.RebookAsync(userId, request);

                if (result.Success)
                    return Ok(result);

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebooking");
                return StatusCode(500, new { Success = false, Message = "An error occurred while rebooking" });
            }
        }

        [HttpPost("check-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityCheckDto request)
        {
            try
            {
                var availability = await _bookingService.CheckAvailabilityAsync(request);
                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability");
                return StatusCode(500, new { Success = false, Message = "An error occurred while checking availability" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetBookingSummary()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var summary = await _bookingService.GetBookingSummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking summary");
                return StatusCode(500, new { Success = false, Message = "An error occurred while fetching summary" });
            }
        }

        [HttpPost("resend-confirmation/{bookingId}")]
        public async Task<IActionResult> ResendConfirmation(int bookingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _bookingService.ResendConfirmationEmailAsync(bookingId, userId);

                if (result.Success)
                    return Ok(result);

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation");
                return StatusCode(500, new { Success = false, Message = "An error occurred while resending confirmation" });
            }
        }
    }
}