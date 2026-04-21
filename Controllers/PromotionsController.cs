using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionsController : ControllerBase
    {
        private readonly PromotionService _promotionService;
        private readonly ILogger<PromotionsController> _logger;

        public PromotionsController(PromotionService promotionService, ILogger<PromotionsController> logger)
        {
            _promotionService = promotionService;
            _logger = logger;
        }

        [HttpGet("valid")]
        [AllowAnonymous]
        public async Task<IActionResult> GetValidPromotions()
        {
            try
            {
                var promotions = await _promotionService.GetValidPromotionsAsync();
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching valid promotions");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromotionDto request)
        {
            try
            {
                var result = await _promotionService.ValidatePromotionAsync(request.PromotionCode, request.BookingAmount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promotion");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost("apply")]
        [Authorize]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionDto request)
        {
            try
            {
                var result = await _promotionService.ApplyPromotionAsync(request.PromotionCode, request.BookingAmount);

                if (result.IsValid)
                    return Ok(result);

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promotion");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpPost("bulk-validate")]
        [AllowAnonymous]
        public async Task<IActionResult> BulkValidatePromotions([FromBody] BulkPromotionDto request)
        {
            try
            {
                var result = await _promotionService.BulkValidatePromotionsAsync(request.PromotionCodes, request.BookingAmount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk validating promotions");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("loyalty/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetLoyaltyPoints(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (currentUserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var loyaltyInfo = await _promotionService.GetLoyaltyPointsAsync(userId);
                return Ok(loyaltyInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching loyalty points");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [HttpGet("seasonal-offers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSeasonalOffers()
        {
            try
            {
                var offers = await _promotionService.GetSeasonalOffersAsync();
                return Ok(offers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching seasonal offers");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionDto request)
        {
            try
            {
                var result = await _promotionService.CreatePromotionAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] CreatePromotionDto request)
        {
            try
            {
                var result = await _promotionService.UpdatePromotionAsync(id, request);

                if (result != null)
                    return Ok(result);

                return NotFound(new { Success = false, Message = "Promotion not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                var result = await _promotionService.DeletePromotionAsync(id);

                if (result)
                    return Ok(new { Success = true, Message = "Promotion deleted successfully" });

                return NotFound(new { Success = false, Message = "Promotion not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion");
                return StatusCode(500, new { Success = false, Message = "An error occurred" });
            }
        }
    }
}