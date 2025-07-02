using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CareerEMSI.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(int senderId, int receiverId, string message)
        {
            // Send to specific user and also to sender (for confirmation)
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new {
                id = 0, // Will be replaced with real ID when saved to DB
                senderId,
                content = message,
                sentAt = DateTime.UtcNow,
                isRead = false
            });
            
            await Clients.User(senderId.ToString()).SendAsync("MessageSent", new {
                id = 0,
                receiverId,
                content = message,
                sentAt = DateTime.UtcNow,
                isRead = false
            });
        }
        
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task MessageRead(int messageId, int senderId)
        {
            await Clients.User(senderId.ToString()).SendAsync("MessageRead", new
            {
                messageId
            });
        }

    }
}