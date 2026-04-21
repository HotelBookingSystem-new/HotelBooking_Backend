using System.Security.Claims;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // ── POST /api/auth/register ───────────────────────────────────────────
        /// <summary>Register a new customer account.</summary>
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitingHelper.AuthPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Register attempt | Email: {Email}", dto.Email);

            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
                return Conflict(result);

            return CreatedAtAction(nameof(GetProfile), null, result);
        }

        // ── POST /api/auth/login ──────────────────────────────────────────────
        /// <summary>Login with email and password. Returns JWT + refresh token.</summary>
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitingHelper.AuthPolicy)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation("Login attempt | Email: {Email}", dto.Email);

            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        // ── POST /api/auth/refresh-token ──────────────────────────────────────
        /// <summary>Obtain a new access token using a valid refresh token.</summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        // ── POST /api/auth/logout ─────────────────────────────────────────────
        /// <summary>Revoke the current refresh token (logout).</summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Invalid token." });

            await _authService.RevokeRefreshTokenAsync(userId);

            _logger.LogInformation("User logged out | UserId: {UserId}", userId);

            return Ok(new { success = true, message = "Logged out successfully." });
        }

        // ── GET /api/auth/profile ─────────────────────────────────────────────
        /// <summary>Get the authenticated user's profile.</summary>
        [HttpGet("profile")]
        [Authorize]
        [EnableRateLimiting(RateLimitingHelper.ApiPolicy)]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Invalid token." });

            var user = await _authService.GetProfileAsync(userId);

            if (user is null)
                return NotFound(new { success = false, message = "User not found." });

            return Ok(new { success = true, data = user });
        }

        // ── PUT /api/auth/profile ─────────────────────────────────────────────
        /// <summary>Update first name, last name, or phone number.</summary>
        [HttpPut("profile")]
        [Authorize]
        [EnableRateLimiting(RateLimitingHelper.ApiPolicy)]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Invalid token." });

            var updated = await _authService.UpdateProfileAsync(userId, dto);
            return Ok(new { success = true, data = updated });
        }

        // ── PUT /api/auth/change-password ─────────────────────────────────────
        /// <summary>Change password for the authenticated user.</summary>
        [HttpPut("change-password")]
        [Authorize]
        [EnableRateLimiting(RateLimitingHelper.AuthPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new { success = false, message = "Invalid token." });

            var success = await _authService.ChangePasswordAsync(userId, dto);

            if (!success)
                return BadRequest(new { success = false, message = "Current password is incorrect." });

            return Ok(new { success = true, message = "Password changed successfully." });
        }

        // ── POST /api/auth/forgot-password ────────────────────────────────────
        /// <summary>Send a password-reset link to the provided email.</summary>
        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimitingHelper.AuthPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Always return 200 — never reveal whether the email exists
            await _authService.ForgotPasswordAsync(dto);

            return Ok(new
            {
                success = true,
                message = "If an account with that email exists, a reset link has been sent."
            });
        }

        // ── POST /api/auth/reset-password ─────────────────────────────────────
        /// <summary>Reset password using the token received by email.</summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimitingHelper.AuthPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _authService.ResetPasswordAsync(dto);

            if (!success)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid or expired reset token."
                });

            return Ok(new { success = true, message = "Password reset successfully. Please log in." });
        }

        // ── GET /api/auth/me ──────────────────────────────────────────────────
        /// <summary>Quick endpoint to verify token validity and return basic user info.</summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var user = await _authService.GetProfileAsync(userId);
            return user is null ? NotFound() : Ok(new { success = true, data = user });
        }

        // ── Private Helper ────────────────────────────────────────────────────

        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");

            return int.TryParse(sub, out var id) ? id : 0;
        }
    }
}