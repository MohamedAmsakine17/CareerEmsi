using CareerEMSI.Hubs;
using CareerEMSI.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace CareerEMSI.Controllers;
using CareerEMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<NotificationHub> _hubContext;


    public PostsController(AppDbContext context, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
        _env = env;
    }

    // GET: api/posts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var posts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Images)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                User = new UserDto
                {
                    Id = p.User.Id,
                    FullName = p.User.FirstName + " " + p.User.LastName,
                    ProfilePictureUrl = p.User.ProfilePictureUrl,
                    Biography = p.User.Biography,
                },
                Type = p.Type,
                ImageUrls = p.Images.Select(i => i.ImageUrl).ToList(),
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();

        return posts;
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PostDto>> GetPost(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Images)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.Id == id)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                User = new UserDto
                {
                    Id = p.User.Id,
                    FullName = p.User.FirstName + " " + p.User.LastName,
                    ProfilePictureUrl = p.User.ProfilePictureUrl
                },
                Type = p.Type,
                ImageUrls = p.Images.Select(i => i.ImageUrl).ToList(),
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId)
            })
            .FirstOrDefaultAsync();

        if (post == null) return NotFound();

        return post;
    }

    // POST: api/posts
    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromForm] CreatePostDto createPostDto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var post = new Post
        {
            Content = createPostDto.Content,
            UserId = userId,
            Type = createPostDto.Type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Handle image uploads
        if (createPostDto.Images != null && createPostDto.Images.Count > 0)
        {
            var images = new List<PostImage>();
            foreach (var imageFile in createPostDto.Images)
            {
                var imageUrl = await SaveImage(imageFile, "post-images");
                images.Add(new PostImage
                {
                    ImageUrl = imageUrl,
                    PostId = post.Id
                });
            }
            _context.PostImages.AddRange(images);
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction("GetPost", new { id = post.Id }, post);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostDto updateDto)
    {
        var userId = GetCurrentUserId();
    
        // Get the existing post with images
        var post = await _context.Posts
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();
        if (post.UserId != userId) return Unauthorized();

        // Update basic fields
        post.Content = updateDto.Content;
        post.Type = updateDto.Type;

        // Handle image deletions
        if (updateDto.ImageIdsToDelete != null && updateDto.ImageIdsToDelete.Count > 0)
        {
            var imagesToDelete = post.Images
                .Where(i => updateDto.ImageIdsToDelete.Contains(i.Id))
                .ToList();

            foreach (var image in imagesToDelete)
            {
                DeleteImage(image.ImageUrl);
                _context.PostImages.Remove(image);
            }
        }

        // Handle new image uploads
        if (updateDto.NewImages != null && updateDto.NewImages.Count > 0)
        {
            foreach (var imageFile in updateDto.NewImages)
            {
                var imageUrl = await SaveImage(imageFile, "post-images");
                _context.PostImages.Add(new PostImage
                {
                    ImageUrl = imageUrl,
                    PostId = post.Id
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private int GetCurrentUserId() => 
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

    // DELETE: api/posts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var post = await _context.Posts
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();
        if (post.UserId != userId) return Unauthorized();

        // Delete images from server
        foreach (var image in post.Images)
        {
            DeleteImage(image.ImageUrl);
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/posts/5/like
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikePost(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var user = await _context.Users
            .Select(u => new {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync(u => u.Id == userId);

        var post = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (post == null) return NotFound();
        
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
        }
        else
        {
            var like = new Like
            {
                PostId = id,
                UserId = userId
            };
            _context.Likes.Add(like);
            
            if (post.UserId != userId) // Don't notify if user likes their own post
            {
                var notification = new Notification
                {
                    UserId = post.UserId,
                    Message = $"liked your post",
                    Type = NotificationType.NewLike,
                    RelatedEntityId = id,
                    PostType = post.Type, 
                    SenderName = $"{user.FirstName} {user.LastName}",
                    SenderImageUrl = user.ProfilePictureUrl
                };
            
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Sending notification to user {post.UserId}");
            
                // Send real-time notification
                await _hubContext.Clients.Group($"user-{post.UserId}")
                    .SendAsync("ReceiveNotification", new {
                        id = notification.Id,
                        message = notification.Message,
                        type = notification.Type.ToString(),
                        isRead = notification.IsRead,
                        createdAt = notification.CreatedAt,
                        relatedEntityId = notification.RelatedEntityId,
                        postType = notification.PostType?.ToString(), 
                        senderName = notification.SenderName,
                        senderImageUrl = notification.SenderImageUrl
                    });
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/posts/5/comments
    [HttpGet("{id}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetPostComments(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var comments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Where(c => c.PostId == id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                User = new UserDto
                {
                    Id = c.User.Id,
                    FullName = c.User.FirstName + " " + c.User.LastName,
                    ProfilePictureUrl = c.User.ProfilePictureUrl
                },
                LikeCount = c.Likes.Count,
                IsLikedByCurrentUser = c.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();

        return comments;
    }

    // POST: api/posts/5/comments
    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(int id, [FromBody] string content)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var user = await _context.Users
            .Select(u => new {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null) return Unauthorized();

        var post = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (post == null) return NotFound("Post not found");

        var comment = new Comment
        {
            Content = content,
            UserId = userId,
            PostId = id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        
        if (post.UserId != userId)
        {
            var notification = new Notification
            {
                UserId = post.UserId,
                Message = $"commented on your post",
                Type = NotificationType.NewComment,
                RelatedEntityId = id,
                PostType = post.Type,
                SenderName = $"{user.FirstName} {user.LastName}",
                SenderImageUrl = user.ProfilePictureUrl
            };
        
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        
            await _hubContext.Clients.Group($"user-{post.UserId}")
                .SendAsync("ReceiveNotification", new {
                    id = notification.Id,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt,
                    relatedEntityId = notification.RelatedEntityId,
                    postType = notification.PostType?.ToString(), 
                    senderName = notification.SenderName,
                    senderImageUrl = notification.SenderImageUrl
                });
        }

        return Ok(new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            User = new UserDto
            {
                Id = userId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                ProfilePictureUrl = user.ProfilePictureUrl
            },
            LikeCount = 0,
            IsLikedByCurrentUser = false
        });
    }

    // POST: api/comments/5/like
    [HttpPost("comments/{id}/like")]
    public async Task<IActionResult> LikeComment(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var user = await _context.Users
            .Select(u => new {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        var comment = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Post) 
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (comment == null) return NotFound("Comment not found");
        
        var existingLike = await _context.CommentLikes
            .FirstOrDefaultAsync(l => l.CommentId == id && l.UserId == userId);

        if (existingLike != null)
        {
            _context.CommentLikes.Remove(existingLike);
        }
        else
        {
            var like = new CommentLike { CommentId = id, UserId = userId };
            _context.CommentLikes.Add(like);
            
            if (comment.UserId != userId)
            {
                var notification = new Notification
                {
                    UserId = comment.UserId,
                    Message = $"liked your comment",
                    Type = NotificationType.NewCommentLike,
                    RelatedEntityId = id,
                    PostType = comment.Post.Type, 
                    SenderName = $"{user.FirstName} {user.LastName}",
                    SenderImageUrl = user.ProfilePictureUrl
                };
            
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            
                await _hubContext.Clients.Group($"user-{comment.UserId}")
                    .SendAsync("ReceiveNotification", new {
                        id = notification.Id,
                        message = notification.Message,
                        type = notification.Type.ToString(),
                        isRead = notification.IsRead,
                        createdAt = notification.CreatedAt,
                        relatedEntityId = notification.RelatedEntityId,
                        postType = notification.PostType?.ToString(), 
                        senderName = notification.SenderName,
                        senderImageUrl = notification.SenderImageUrl,
                        // Optional: include post context if needed
                        postId = comment.PostId  
                    });
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> SaveImage(IFormFile imageFile, string folderName)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", folderName);
        var filePath = Path.Combine(folderPath, fileName);

        Directory.CreateDirectory(folderPath);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return $"/uploads/{folderName}/{fileName}";
    }

    private void DeleteImage(string imageUrl)
    {
        var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }
    
    [HttpPut("comments/{id}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto updateDto)
    {
        var userId = GetCurrentUserId();
        var comment = await _context.Comments.FindAsync(id);

        if (comment == null) return NotFound();
        if (comment.UserId != userId) return Unauthorized();

        comment.Content = updateDto.Content;
        _context.Entry(comment).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

// DELETE: api/posts/comments/5
    [HttpDelete("comments/{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = GetCurrentUserId();
        var comment = await _context.Comments.FindAsync(id);

        if (comment == null) return NotFound();
        if (comment.UserId != userId) return Unauthorized();

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // GET: api/posts/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPostsByUser(int userId)
    {
        return await _context.Posts
            .Where(p => p.UserId == userId)
            .Include(p => p.User)
            .Include(p => p.Images)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                User = new UserDto
                {
                    Id = p.User.Id,
                    FullName = p.User.FirstName + " " + p.User.LastName,
                    ProfilePictureUrl = p.User.ProfilePictureUrl,
                    Biography = p.User.Biography,
                },
                Type = p.Type,
                ImageUrls = p.Images.Select(i => i.ImageUrl).ToList(),
                LikeCount = p.Likes.Count,
                CommentCount = p.Comments.Count,
                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == GetCurrentUserId())
            })
            .ToListAsync();
    }
}