using ChatAppApi.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        var users = _context.Users
            .Select(u => new
            {
                u.Id,
                u.Name
            })
            .ToList();

        return Ok(users);
    }
}