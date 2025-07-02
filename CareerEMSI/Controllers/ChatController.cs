using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CareerEMSI.Hubs;
using CareerEMSI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CareerEMSI.Models.Enums;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHubContext<NotificationHub> _notificationhubContext;

    public ChatController(AppDbContext context, IHubContext<ChatHub> hubContext, IHubContext<NotificationHub> notificationHubContext)
    {
        _context = context;
        _hubContext = hubContext;
        _notificationhubContext = notificationHubContext;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
    {
        var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var sender = await _context.Users
            .Select(u => new {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync(u => u.Id == senderId);
        
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = messageDto.ReceiverId,
            Content = messageDto.Content,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Send via SignalR
        await _hubContext.Clients.User(messageDto.ReceiverId.ToString())
            .SendAsync("ReceiveMessage", new {
                id = message.Id,
                senderId = message.SenderId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = false
            });

        await _hubContext.Clients.User(senderId.ToString())
            .SendAsync("ReceiveMessage", new {
                id = message.Id,
                senderId = message.SenderId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = false
            });
        
        if (senderId != messageDto.ReceiverId)
        {
            var notification = new Notification
            {
                UserId = messageDto.ReceiverId,
                Message = "sent you a message",
                Type = NotificationType.NewMessage,
                RelatedEntityId = message.Id,
                SenderName = $"{sender.FirstName} {sender.LastName}",
                SenderImageUrl = sender.ProfilePictureUrl
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _notificationhubContext.Clients.Group($"user-{messageDto.ReceiverId}")
                .SendAsync("ReceiveNotification", new {
                    id = notification.Id,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt,
                    relatedEntityId = notification.RelatedEntityId,
                    postType = (string)null,
                    senderName = notification.SenderName,
                    senderImageUrl = notification.SenderImageUrl
                });
        }

        return Ok();
    }


    [HttpGet("users")]
    public async Task<IActionResult> GetChatUsers()
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var users = await _context.Messages
            .Include(m => m.Sender)    // Include sender
            .Include(m => m.Receiver)  // Include receiver
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .Select(m => m.SenderId == currentUserId ? m.Receiver : m.Sender)
            .Distinct()
            .Select(u => new
            {
                u.Id,
                Username = u.FirstName + " " + u.LastName, // Combine first/last name
                u.Email,
                u.ProfilePictureUrl,
                LastMessage = _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == u.Id) || 
                                (m.SenderId == u.Id && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(users);
    }
    
    [HttpGet("history/{otherUserId}")]
    public async Task<IActionResult> GetMessageHistory(int otherUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var messages = await _context.Messages
            .Where(m =>
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        // ✅ Mark messages as read if they were received by current user and not yet read
        var unreadMessages = messages
            .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
            .ToList();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Notify senders via SignalR
            var senderIds = unreadMessages.Select(m => m.SenderId).Distinct();

            foreach (var senderId in senderIds)
            {
                var readMessageIds = unreadMessages
                    .Where(m => m.SenderId == senderId)
                    .Select(m => m.Id)
                    .ToArray();

                await _hubContext.Clients.User(senderId.ToString())
                    .SendAsync("MessagesRead", readMessageIds);
            }
        }


        return Ok(messages.Select(m => new
        {
            m.Id,
            m.SenderId,
            m.ReceiverId,
            m.Content,
            m.SentAt,
            m.IsRead
        }));
    }

}

public class MessageDto
{
    public int ReceiverId { get; set; }
    public string Content { get; set; }
}