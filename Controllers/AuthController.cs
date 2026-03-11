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
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto)
    {
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully");
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null)
            return Unauthorized("Invalid email");

        bool validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!validPassword)
            return Unauthorized("Invalid password");


        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            userId = user.Id,
            name = user.Name
        });
    }



    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured"))
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}