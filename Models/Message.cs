namespace ChatAppApi.Models;

public class Message
{
    public int Id { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public required string Text { get; set; }

    public DateTime SentAt { get; set; }
}