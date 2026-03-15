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
// LOGIN
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDTO dto)
{
    try
    {
        if (dto == null)
            return BadRequest("Request body is empty");

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == dto.Email.ToLower());

        if (user == null)
            return Unauthorized(new { message = "Invalid email" });

        if (string.IsNullOrEmpty(user.PasswordHash))
            return StatusCode(500, "PasswordHash is null");

        bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!validPassword)
            return Unauthorized(new { message = "Invalid password" });

        var jwtSettings = _configuration.GetSection("Jwt");

        var keyValue = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if (string.IsNullOrEmpty(keyValue))
            return StatusCode(500, "JWT Key missing");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

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
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}
}