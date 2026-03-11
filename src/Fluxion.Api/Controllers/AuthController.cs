using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fluxion.Core.Models;
using Fluxion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Fluxion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FluxionDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(FluxionDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("Username already exists");

        var learner = new LearnerProfile 
        { 
            DisplayName = request.Username,
            PreferredFormat = ContentFormat.Text 
        };
        _context.Learners.Add(learner);

        var user = new ApplicationUser
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            LearnerProfile = learner
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users
            .Include(u => u.LearnerProfile)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password");

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse(
            token, 
            user.Username, 
            user.LearnerProfile?.Id ?? Guid.Empty));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Server-side acknowledgment of logout.
        // Actual token invalidation requires client to discard the token.
        // For stateless JWTs, the short 1-hour lifetime limits exposure.
        return Ok(new { Message = "Logged out successfully" });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Missing required configuration: Jwt:Key.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("LearnerId", user.LearnerProfileId?.ToString() ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "Fluxion",
            audience: _config["Jwt:Audience"] ?? "FluxionLearners",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "Username may only contain letters, numbers, underscores, hyphens, and dots")]
    string Username,

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    string Password
);

public record LoginRequest(
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters")]
    string Username,

    [Required(ErrorMessage = "Password is required")]
    [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    string Password
);

public record AuthResponse(string Token, string Username, Guid LearnerId);
