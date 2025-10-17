using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StrideLedger.Models;
using StrideLedger.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace StrideLedger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ShoeContext _context;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ShoeContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // Helper: generate access token
        private string GenerateAccessToken(IdentityUser user)
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                          DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "StrideLedger",
                audience: "StrideLedgerUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helper: create a refresh token string (random)
        private string GenerateRefreshTokenString(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(bytes);
        }

        // Helper: hash token (SHA256)
        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); // .NET 5+ method
        }

        private RefreshToken CreateRefreshTokenEntity(string token, string userId, string? ip)
        {
            return new RefreshToken
            {
                TokenHash = HashToken(token),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7), // adjust lifetime as required
                Created = DateTime.UtcNow,
                CreatedByIp = ip
            };
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshTokenString = GenerateRefreshTokenString();
                var refreshToken = CreateRefreshTokenEntity(refreshTokenString, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                return StatusCode(201, new
                {
                    token = accessToken,
                    refreshToken = refreshTokenString,
                    message = "User registered and logged in"
                });
            }

            var errorEnvelope = new
            {
                success = false,
                error = "Registration failed",
                fields = new Dictionary<string, string>()
            };

            foreach (var err in result.Errors)
            {
                if (err.Code == "DuplicateUserName")
                {
                    errorEnvelope = new
                    {
                        success = false,
                        error = "Email already in use",
                        fields = new Dictionary<string, string> { { "email", "already in use" } }
                    };
                    break;
                }
            }

            return BadRequest(errorEnvelope);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var accessToken = GenerateAccessToken(user);
                var refreshTokenString = GenerateRefreshTokenString();
                var refreshToken = CreateRefreshTokenEntity(refreshTokenString, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    token = accessToken,
                    refreshToken = refreshTokenString
                });
            }
            return Unauthorized(new
            {
                success = false,
                error = "Invalid email or password"
            });
        }

        public class RefreshRequest
        {
            public string Token { get; set; } = null!;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token)) return BadRequest(new { error = "Token is required" });

            var tokenHash = HashToken(request.Token);
            var existing = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (existing == null || !existing.IsActive)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }

            // find user
            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null) return Unauthorized(new { error = "Invalid token user" });

            // rotate: revoke existing, create new
            existing.Revoked = DateTime.UtcNow;
            existing.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var newRefreshTokenString = GenerateRefreshTokenString();
            var newRefreshToken = CreateRefreshTokenEntity(newRefreshTokenString, user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
            existing.ReplacedByToken = newRefreshToken.TokenHash;

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var newAccessToken = GenerateAccessToken(user);

            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshTokenString
            });
        }

        public class RevokeRequest
        {
            public string Token { get; set; } = null!;
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Token)) return BadRequest(new { error = "Token is required" });

            var tokenHash = HashToken(request.Token);
            var existing = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (existing == null || !existing.IsActive)
            {
                return NotFound(new { error = "Token not found or already revoked" });
            }

            existing.Revoked = DateTime.UtcNow;
            existing.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _context.SaveChangesAsync();

            return Ok(new { message = "Token revoked" });
        }
    }
}