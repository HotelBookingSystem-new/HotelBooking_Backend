using System;
using System.Threading.Tasks;
using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
        Task<UserDto?> GetProfileAsync(int userId);
        Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> RevokeRefreshTokenAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IJwtHelper _jwt;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;

        public AuthService(
            ApplicationDbContext db,
            IJwtHelper jwt,
            IEmailService email,
            IConfiguration config)
        {
            _db = db;
            _jwt = jwt;
            _email = email;
            _config = config;
        }

        // ── Register ──────────────────────────────────────────────────────────

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Duplicate e-mail check
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
                return Fail("An account with this email already exists.");

            var user = new User
            {
                Email = dto.Email.ToLower().Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Role = "Customer",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                LoyaltyTier = "Bronze",
                LoyaltyPoints = 0,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Send welcome email (fire-and-forget; do not block registration)
            _ = _email.SendWelcomeEmailAsync(user);

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();
            await SaveRefreshTokenAsync(user, refreshToken);

            return Success(accessToken, refreshToken, user, "Registration successful. Welcome!");
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Fail("Invalid email or password.");

            user.LastLoginAt = DateTime.UtcNow;

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();
            await SaveRefreshTokenAsync(user, refreshToken);

            return Success(accessToken, refreshToken, user, "Login successful.");
        }

        // ── Refresh Token ─────────────────────────────────────────────────────

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiry > DateTime.UtcNow &&
                u.IsActive);

            if (user is null)
                return Fail("Invalid or expired refresh token.");

            var newAccessToken = _jwt.GenerateAccessToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();
            await SaveRefreshTokenAsync(user, newRefreshToken);

            return Success(newAccessToken, newRefreshToken, user, "Token refreshed.");
        }

        // ── Change Password ───────────────────────────────────────────────────

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.RefreshToken = null; // invalidate all sessions
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Forgot Password ───────────────────────────────────────────────────

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);

            // Always return true (don't reveal account existence)
            if (user is null) return true;

            user.PasswordResetToken = Guid.NewGuid().ToString("N");
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync();

            await _email.SendPasswordResetEmailAsync(user, user.PasswordResetToken);
            return true;
        }

        // ── Reset Password ────────────────────────────────────────────────────

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.Email.ToLower() &&
                u.PasswordResetToken == dto.Token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user is null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Profile ───────────────────────────────────────────────────────────

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user is null ? null : MapToUserDto(user);
        }

        public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return null;

            if (dto.FirstName is not null) user.FirstName = dto.FirstName.Trim();
            if (dto.LastName is not null) user.LastName = dto.LastName.Trim();
            if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber.Trim();

            await _db.SaveChangesAsync();
            return MapToUserDto(user);
        }

        // ── Revoke Refresh Token (logout) ─────────────────────────────────────

        public async Task<bool> RevokeRefreshTokenAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private async Task SaveRefreshTokenAsync(User user, string token)
        {
            var expiryDays = int.TryParse(_config["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;
            user.RefreshToken = token;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(expiryDays);
            await _db.SaveChangesAsync();
        }

        private AuthResponseDto Success(string token, string refreshToken, User user, string message) =>
            new()
            {
                Success = true,
                Message = message,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60),
                User = MapToUserDto(user),
            };

        private static AuthResponseDto Fail(string message) =>
            new() { Success = false, Message = message };

        private static UserDto MapToUserDto(User u) => new()
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            LoyaltyPoints = u.LoyaltyPoints,
            LoyaltyTier = u.LoyaltyTier,
            IsEmailVerified = u.IsEmailVerified,
        };
    }
}