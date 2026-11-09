using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthAPI.DBcontext;
using AuthAPI.Models;
using AuthAPI.Models.DTO;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers
{
    [Route("api/AuthAPI")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private static readonly string TimingDummyHash = new PasswordHasher<UserModel>().HashPassword(new UserModel(), "timing-equalizer");

        private readonly AuthDbContext _db;
        private readonly TokenService _tokens;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<UserModel> _hasher = new();

        public AuthController(AuthDbContext db, TokenService tokens, IConfiguration config)
        {
            _db = db;
            _tokens = tokens;
            _config = config;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponseDTO>> Register([FromBody] RegisterRequestDTO request)
        {
            return await CreateAccountAsync(request, UserRole.User);
        }

        //[HttpPost("register/admin")]
        //[EnableRateLimiting("auth")]
        //public async Task<ActionResult<AuthResponseDTO>> RegisterAdmin([FromBody] AdminRegisterRequestDTO request)
        //{
        //    var setupKey = _config["Admin:SetupKey"];
        //    if (string.IsNullOrEmpty(setupKey) ||
        //        !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(request.SetupKey), Encoding.UTF8.GetBytes(setupKey)))
        //    {
        //        return Unauthorized("Invalid setup key.");
        //    }

        //    return await CreateAccountAsync(request, UserRole.Admin);
        //}


        [HttpPost("register/admin")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponseDTO>> RegisterAdmin([FromBody] AdminRegisterRequestDTO request)
        {
            // Allow only the first admin account to be created.
            if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                return Conflict("An admin account already exists.");
            }

            var setupKey = _config["Admin:SetupKey"];

            if (string.IsNullOrEmpty(setupKey) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(request.SetupKey),
                    Encoding.UTF8.GetBytes(setupKey)))
            {
                return Unauthorized("Invalid setup key.");
            }

            return await CreateAccountAsync(request, UserRole.Admin);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _hasher.VerifyHashedPassword(new UserModel(), TimingDummyHash, request.Password);
                return Unauthorized("Invalid email or password.");
            }

            if (!VerifyPassword(user, request.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(ToResponse(user));
        }

        [HttpGet("all")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Role = u.Role.ToString(),
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrEmpty(jti) || !long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var expSeconds))
            {
                return Unauthorized();
            }

            await _db.RevokedTokens.Where(r => r.ExpiresAt < DateTime.UtcNow).ExecuteDeleteAsync();

            _db.RevokedTokens.Add(new RevokedToken
            {
                Jti = jti,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
            }

            return NoContent();
        }

        private async Task<ActionResult<AuthResponseDTO>> CreateAccountAsync(RegisterRequestDTO request, UserRole role)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict("Email is already registered.");
            }

            var user = new UserModel
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Role = role
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                return Conflict("Email is already registered.");
            }

            return CreatedAtAction(nameof(Login), ToResponse(user));
        }

        private bool VerifyPassword(UserModel user, string password)
        {
            return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
        }

        private AuthResponseDTO ToResponse(UserModel user)
        {
            var (token, expiresAt) = _tokens.CreateToken(user);
            return new AuthResponseDTO
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = expiresAt
            };
        }
    }
}
