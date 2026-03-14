using ChatAppApi.Data;
using ChatAppApi.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ChatAppApi.Hubs;

[Authorize]
public class ChatHub : Hub{
    private static Dictionary<int, string> OnlineUsers = new();

    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext != null && httpContext.Request.Query.TryGetValue("userId", out var userId))
        {
            if (int.TryParse(userId, out int id))
            {
                OnlineUsers[id] = Context.ConnectionId;

                // Notify others
                await Clients.All.SendAsync("UserOnline", id);

                // Send full online list to the new user
                await Clients.Caller.SendAsync("OnlineUsers", OnlineUsers.Keys);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = OnlineUsers.FirstOrDefault(x => x.Value == Context.ConnectionId);

        if (user.Key != 0)
        {
            OnlineUsers.Remove(user.Key);

            await Clients.All.SendAsync("UserOffline", user.Key);
        }

        await base.OnDisconnectedAsync(exception);
    }


    // SEND MESSAGE
    public async Task SendPrivateMessage(int senderId, int receiverId, string message)
    {
        var msg = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Text = message,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();


        if (OnlineUsers.ContainsKey(receiverId))
        {
            await Clients.Client(OnlineUsers[receiverId])
                .SendAsync("ReceiveMessage", senderId, receiverId, message, msg.SentAt);
        }

        if (OnlineUsers.ContainsKey(senderId))
        {
            await Clients.Client(OnlineUsers[senderId])
                .SendAsync("ReceiveMessage", senderId, receiverId, message, msg.SentAt);
        }
    }


    // TYPING INDICATOR
    public async Task Typing(int senderId, int receiverId)
    {
        if (OnlineUsers.ContainsKey(receiverId))
        {
            await Clients.Client(OnlineUsers[receiverId])
                .SendAsync("UserTyping", senderId);
        }
    }
}