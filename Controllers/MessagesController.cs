using ChatAppApi.Data;
using ChatAppApi.DTOs;
using ChatAppApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MessagesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(MessageDTO dto)
    {
        var message = new Message
        {
            SenderId = dto.SenderId,
            ReceiverId = dto.ReceiverId,
            Text = dto.Text,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetMessages(int userId)
    {
        var messages = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet("conversation")]
public async Task<IActionResult> GetConversation(int user1, int user2)
{
    var messages = await _context.Messages
        .Where(m =>
            (m.SenderId == user1 && m.ReceiverId == user2) ||
            (m.SenderId == user2 && m.ReceiverId == user1)
        )
        .OrderBy(m => m.SentAt)
        .ToListAsync();

    return Ok(messages);
}
}