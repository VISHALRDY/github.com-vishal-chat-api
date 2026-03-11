namespace ChatAppApi.DTOs;

public class MessageDTO
{
    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public required string Text { get; set; }
}