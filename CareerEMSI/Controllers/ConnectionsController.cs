// ConnectionsController.cs
using System.Security.Claims;
using CareerEMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CareerEMSI.Models.Enums;
using Microsoft.AspNetCore.SignalR;


namespace CareerEMSI.Controllers;

[Route("api/connections")]
[ApiController]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;


    public ConnectionsController(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;

    }

    // GET: api/connections - Get all connections for current user
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConnectionResultDto>>> GetConnections()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var connections = await _context.Connections
            .Include(c => c.Requester)
            .Include(c => c.Receiver)
            .Where(c => c.RequesterId == userId || c.ReceiverId == userId)
            .Where(c => c.Status == ConnectionStatus.Accepted)
            .Select(c => new ConnectionResultDto
            {
                ConnectionId = c.ConnectionId,
                RequesterId = c.RequesterId,
                ReceiverId = c.ReceiverId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                Requester = new BasicUserDto
                {
                    Id = c.Requester.Id,
                    FirstName = c.Requester.FirstName,
                    LastName = c.Requester.LastName,
                    ProfilePictureUrl = c.Requester.ProfilePictureUrl
                },
                Receiver = new BasicUserDto
                {
                    Id = c.Receiver.Id,
                    FirstName = c.Receiver.FirstName,
                    LastName = c.Receiver.LastName,
                    ProfilePictureUrl = c.Receiver.ProfilePictureUrl
                }
            })
            .ToListAsync();

        return connections;
    }

    // GET: api/connections/pending - Get pending connection requests
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<ConnectionResultDto>>> GetPendingConnections()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var pendingConnections = await _context.Connections
            .Include(c => c.Requester)
            .Where(c => c.ReceiverId == userId && c.Status == ConnectionStatus.Pending)
            .Select(c => new ConnectionResultDto
            {
                ConnectionId = c.ConnectionId,
                RequesterId = c.RequesterId,
                ReceiverId = c.ReceiverId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                Requester = new BasicUserDto
                {
                    Id = c.Requester.Id,
                    FirstName = c.Requester.FirstName,
                    LastName = c.Requester.LastName,
                    ProfilePictureUrl = c.Requester.ProfilePictureUrl
                },
                Receiver = new BasicUserDto
                {
                    Id = c.Receiver.Id,
                    FirstName = c.Receiver.FirstName,
                    LastName = c.Receiver.LastName,
                    ProfilePictureUrl = c.Receiver.ProfilePictureUrl
                }
            })
            .ToListAsync();

        return pendingConnections;
    }

    // POST: api/connections - Send a connection request
    [HttpPost]
public async Task<ActionResult<ConnectionResultDto>> CreateConnection(ConnectionDto connectionDto)
{
    var requesterId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
    var requester = await _context.Users
        .Select(u => new {
            u.Id,
            u.FirstName,
            u.LastName,
            u.ProfilePictureUrl
        })
        .FirstOrDefaultAsync(u => u.Id == requesterId);
    
    // Check if receiver exists
    var receiver = await _context.Users.FindAsync(connectionDto.ReceiverId);
    if (receiver == null) return NotFound("User not found");

    // Check if not trying to connect with self
    if (requesterId == connectionDto.ReceiverId)
        return BadRequest("Cannot connect with yourself");

    // Check if connection already exists
    var existingConnection = await _context.Connections
        .FirstOrDefaultAsync(c => 
            (c.RequesterId == requesterId && c.ReceiverId == connectionDto.ReceiverId) ||
            (c.RequesterId == connectionDto.ReceiverId && c.ReceiverId == requesterId));

    if (existingConnection != null)
    {
        if (existingConnection.Status == ConnectionStatus.Blocked)
            return BadRequest("Connection is blocked");
        
        if (existingConnection.Status == ConnectionStatus.Pending)
            return BadRequest("Connection request already pending");
        
        if (existingConnection.Status == ConnectionStatus.Accepted)
            return BadRequest("Already connected");
    }

    var connection = new Connection
    {
        RequesterId = requesterId,
        ReceiverId = connectionDto.ReceiverId,
        Status = ConnectionStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    _context.Connections.Add(connection);
    await _context.SaveChangesAsync();
    
    var notification = new Notification
    {
        UserId = connectionDto.ReceiverId, // Notify the receiver
        Message = $"sent you a connection request",
        Type = NotificationType.ConnectionRequest,
        RelatedEntityId = connection.ConnectionId,
        SenderName = $"{requester.FirstName} {requester.LastName}",
        SenderImageUrl = requester.ProfilePictureUrl
    };
    
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();

    await _hubContext.Clients.Group($"user-{connectionDto.ReceiverId}")
        .SendAsync("ReceiveNotification", new {
            id = notification.Id,
            message = notification.Message,
            type = notification.Type.ToString(),
            isRead = notification.IsRead,
            createdAt = notification.CreatedAt,
            relatedEntityId = notification.RelatedEntityId,
            senderName = notification.SenderName,
            senderImageUrl = notification.SenderImageUrl,
            // Additional connection-specific data
            connectionId = connection.ConnectionId,
            connectionStatus = connection.Status.ToString()
        });
    
    // Load the full connection with user data
    var createdConnection = await _context.Connections
        .Include(c => c.Requester)
        .Include(c => c.Receiver)
        .FirstOrDefaultAsync(c => c.ConnectionId == connection.ConnectionId);

 
    
    // Map to DTO
    var result = new ConnectionResultDto
    {
        ConnectionId = createdConnection.ConnectionId,
        RequesterId = createdConnection.RequesterId,
        ReceiverId = createdConnection.ReceiverId,
        Status = createdConnection.Status,
        CreatedAt = createdConnection.CreatedAt,
        Requester = new BasicUserDto
        {
            Id = createdConnection.Requester.Id,
            FirstName = createdConnection.Requester.FirstName,
            LastName = createdConnection.Requester.LastName,
            ProfilePictureUrl = createdConnection.Requester.ProfilePictureUrl
        },
        Receiver = new BasicUserDto
        {
            Id = createdConnection.Receiver.Id,
            FirstName = createdConnection.Receiver.FirstName,
            LastName = createdConnection.Receiver.LastName,
            ProfilePictureUrl = createdConnection.Receiver.ProfilePictureUrl
        }
    };

    return Created($"/api/connections/{connection.ConnectionId}", result);
}

    // PUT: api/connections/5 - Respond to a connection request
    [HttpPut("{id}")]
    public async Task<IActionResult> RespondToConnection(int id, ConnectionResponseDto responseDto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var connection = await _context.Connections
            .Include(c => c.Requester)
            .Include(c => c.Receiver)
            .FirstOrDefaultAsync(c => c.ConnectionId == id);
        
        if (connection == null) return NotFound();

        // Only the receiver can respond
        if (connection.ReceiverId != userId)
            return Unauthorized("Only the receiver can respond to this connection");

        if (connection.Status != ConnectionStatus.Pending)
            return BadRequest("Connection is not in pending status");

        connection.Status = responseDto.Accept ? ConnectionStatus.Accepted : ConnectionStatus.Declined;
    
        await _context.SaveChangesAsync();

        if (connection.Status == ConnectionStatus.Accepted)
        {
            var notification = new Notification
            {
                UserId = connection.RequesterId,
                Message = $"accepted your connection request",
                Type = NotificationType.ConnectionAccepted,
                RelatedEntityId = connection.ConnectionId,
                SenderName = $"{connection.Receiver.FirstName} {connection.Receiver.LastName}",
                SenderImageUrl = connection.Receiver.ProfilePictureUrl
            };
        
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        
            await _hubContext.Clients.Group($"user-{connection.RequesterId}")
                .SendAsync("ReceiveNotification", new {
                    id = notification.Id,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt,
                    relatedEntityId = notification.RelatedEntityId,
                    senderName = notification.SenderName,
                    senderImageUrl = notification.SenderImageUrl,
                    connectionId = connection.ConnectionId,
                    connectionStatus = connection.Status.ToString()
                });
        }
        
        return NoContent();
    }

    // DELETE: api/connections/5 - Delete a connection
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConnection(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var connection = await _context.Connections.FindAsync(id);
        if (connection == null) return NotFound();

        // Only requester or receiver can delete
        if (connection.RequesterId != userId && connection.ReceiverId != userId)
            return Unauthorized();

        _context.Connections.Remove(connection);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ConnectionResultDto> GetConnection(int id)
    {
        var connection = await _context.Connections
            .Include(c => c.Requester)
            .Include(c => c.Receiver)
            .FirstOrDefaultAsync(c => c.ConnectionId == id);

        if (connection == null) return null;

        return new ConnectionResultDto
        {
            ConnectionId = connection.ConnectionId,
            RequesterId = connection.RequesterId,
            ReceiverId = connection.ReceiverId,
            Status = connection.Status,
            CreatedAt = connection.CreatedAt,
            Requester = new BasicUserDto
            {
                Id = connection.Requester.Id,
                FirstName = connection.Requester.FirstName,
                LastName = connection.Requester.LastName,
                ProfilePictureUrl = connection.Requester.ProfilePictureUrl
            },
            Receiver = new BasicUserDto
            {
                Id = connection.Receiver.Id,
                FirstName = connection.Receiver.FirstName,
                LastName = connection.Receiver.LastName,
                ProfilePictureUrl = connection.Receiver.ProfilePictureUrl
            }
        };
    }
    
    // GET: api/connections/received
    [HttpGet("received")]
    public async Task<ActionResult<IEnumerable<ConnectionResultDto>>> GetReceivedConnections()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var receivedConnections = await _context.Connections
            .Include(c => c.Requester)  // Include the requester details
            .Include(c => c.Receiver)   // Include the receiver details (you)
            .Where(c => c.ReceiverId == userId)  // Only connections where you're the receiver
            .Select(c => new ConnectionResultDto
            {
                ConnectionId = c.ConnectionId,
                RequesterId = c.RequesterId,
                ReceiverId = c.ReceiverId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                Requester = new BasicUserDto
                {
                    Id = c.Requester.Id,
                    FirstName = c.Requester.FirstName,
                    LastName = c.Requester.LastName,
                    ProfilePictureUrl = c.Requester.ProfilePictureUrl
                },
                Receiver = new BasicUserDto
                {
                    Id = c.Receiver.Id,
                    FirstName = c.Receiver.FirstName,
                    LastName = c.Receiver.LastName,
                    ProfilePictureUrl = c.Receiver.ProfilePictureUrl
                }
            })
            .ToListAsync();

        return receivedConnections;
    }
    
    // GET: api/connections/sent
    [HttpGet("sent")]
    public async Task<ActionResult<IEnumerable<ConnectionResultDto>>> GetSentConnections()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var sentConnections = await _context.Connections
            .Include(c => c.Requester)  // That's you
            .Include(c => c.Receiver)   // The person you sent the request to
            .Where(c => c.RequesterId == userId)  // Only connections you initiated
            .Select(c => new ConnectionResultDto
            {
                ConnectionId = c.ConnectionId,
                RequesterId = c.RequesterId,
                ReceiverId = c.ReceiverId,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                Requester = new BasicUserDto
                {
                    Id = c.Requester.Id,
                    FirstName = c.Requester.FirstName,
                    LastName = c.Requester.LastName,
                    ProfilePictureUrl = c.Requester.ProfilePictureUrl
                },
                Receiver = new BasicUserDto
                {
                    Id = c.Receiver.Id,
                    FirstName = c.Receiver.FirstName,
                    LastName = c.Receiver.LastName,
                    ProfilePictureUrl = c.Receiver.ProfilePictureUrl
                }
            })
            .ToListAsync();

        return sentConnections;
    }
    [HttpGet("accepted")]
    public async Task<ActionResult<IEnumerable<BasicUserDto>>> GetAcceptedConnections()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        // Get connections where user is requester or receiver and status is accepted
        var connections = await _context.Connections
            .Include(c => c.Requester)
            .Include(c => c.Receiver)
            .Where(c => c.Status == ConnectionStatus.Accepted &&
                        (c.RequesterId == userId || c.ReceiverId == userId))
            .ToListAsync();

        // Project to the other user in each connection
        var acceptedUsers = connections
            .Select(c => c.RequesterId == userId ? c.Receiver : c.Requester)
            .Where(u => u.Id != userId) // Should not be necessary now, but for safety
            .Distinct()
            .Select(u => new BasicUserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                ProfilePictureUrl = u.ProfilePictureUrl,
                ProfileBackgroundUrl = u.ProfileBackgroundUrl
            })
            .ToList();

        return Ok(acceptedUsers);
    }


}