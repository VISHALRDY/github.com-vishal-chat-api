using ChatAppApi.Data;
using ChatAppApi.Models;
using ChatAppApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto)
    {
        var userExists = await _context.Users
            .AnyAsync(x => x.Email == dto.Email);

        if (userExists)
            return BadRequest("Email already registered");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User registered successfully" });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == dto.Email.ToLower());

        if (user == null)
            return Unauthorized(new { message = "Invalid email" });

        bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!validPassword)
            return Unauthorized(new { message = "Invalid password" });

        // Read JWT settings (works locally and on Azure)
        var keyValue =
            _configuration["Jwt:Key"] ??
            _configuration["Jwt__Key"];

        var issuer =
            _configuration["Jwt:Issuer"] ??
            _configuration["Jwt__Issuer"];

        var audience =
            _configuration["Jwt:Audience"] ??
            _configuration["Jwt__Audience"];

        if (string.IsNullOrEmpty(keyValue))
            return StatusCode(500, "JWT key missing in configuration");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            userId = user.Id,
            name = user.Name
        });
    }
}
