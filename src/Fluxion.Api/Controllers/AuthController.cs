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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // We'll need BCrypt package
            LearnerProfile = learner
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
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

    private string GenerateJwtToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "SUPER_SECRET_FLUXION_KEY_32_CHARS_LONG"));
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
            expires: DateTime.Now.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, string Username, Guid LearnerId);
