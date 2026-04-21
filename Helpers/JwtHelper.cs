using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelManagement.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HotelManagement.Helpers
{
    public interface IJwtHelper
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        int GetUserIdFromToken(string token);
    }

    public class JwtHelper : IJwtHelper
    {
        private readonly IConfiguration _config;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtHelper(IConfiguration config)
        {
            _config = config;
            _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
            _issuer = config["Jwt:Issuer"] ?? "HotelBookingAPI";
            _audience = config["Jwt:Audience"] ?? "HotelBookingClient";
            _expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var m) ? m : 60;
        }

        /// <summary>Generates a signed JWT access token for the given user.</summary>
        public string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new(ClaimTypes.Role,               user.Role),
                new("firstName",                   user.FirstName),
                new("lastName",                    user.LastName),
                new("loyaltyTier",                 user.LoyaltyTier),
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Generates a cryptographically random refresh token.</summary>
        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>Validates the token signature & expiry. Returns principal or null.</summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Extracts the UserId (sub claim) from a token string.</summary>
        public int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            return subClaim != null && int.TryParse(subClaim.Value, out var id) ? id : 0;
        }
    }
}