namespace CareerEMSI.Controllers;
using CareerEMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CareerEMSI.Models.Enums;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InternshipPostsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public InternshipPostsController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: api/internshipposts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InternshipPostDto>>> GetInternshipPosts()
    {
        var userId = GetCurrentUserId();
        
        return await _context.InternshipPosts
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InternshipPostDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Location = i.Location,
                ExpiryDate = i.ExpiryDate,
                Type = i.Type,
                InternshipType = i.InternshipType,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                User = UserDto.FromUser(i.User),
                LikeCount = i.Likes.Count,
                CommentCount = i.Comments.Count,
                IsLikedByCurrentUser = i.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }

    // GET: api/internshipposts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<InternshipPostDto>> GetInternshipPost(int id)
    {
        var userId = GetCurrentUserId();
        
        var internshipPost = await _context.InternshipPosts
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .Where(i => i.Id == id)
            .Select(i => new InternshipPostDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Location = i.Location,
                ExpiryDate = i.ExpiryDate,
                Type = i.Type,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                User = UserDto.FromUser(i.User),
                LikeCount = i.Likes.Count,
                CommentCount = i.Comments.Count,
                IsLikedByCurrentUser = i.Likes.Any(l => l.UserId == userId)
            })
            .FirstOrDefaultAsync();

        if (internshipPost == null) return NotFound();

        return internshipPost;
    }

    // POST: api/internshipposts
    [HttpPost]
    public async Task<ActionResult<InternshipPostDto>> CreateInternshipPost([FromForm] CreateInternshipPostDto createDto)
    {
        var internshipPost = new InternshipPost
        {
            Title = createDto.Title,
            Content = createDto.Content,
            Location = createDto.Location,
            ExpiryDate = createDto.ExpiryDate,
            Type = PostType.Internship, 
            InternshipType = createDto.InternshipType,
            UserId = GetCurrentUserId(),
            CreatedAt = DateTime.UtcNow
        };

        if (createDto.Image != null)
        {
            internshipPost.ImageUrl = await SaveImage(createDto.Image, "internship-images");
        }

        _context.InternshipPosts.Add(internshipPost);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInternshipPost), new { id = internshipPost.Id }, MapToDto(internshipPost));
    }

    // PUT: api/internshipposts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInternshipPost(int id, [FromForm] UpdateInternshipPostDto updateDto)
    {
        var internshipPost = await _context.InternshipPosts.FindAsync(id);
        if (internshipPost == null) return NotFound();

        if (internshipPost.UserId != GetCurrentUserId())
            return Unauthorized();

        // Update basic fields
        internshipPost.Title = updateDto.Title;
        internshipPost.Content = updateDto.Content;
        internshipPost.Location = updateDto.Location;
        internshipPost.ExpiryDate = updateDto.ExpiryDate;
        internshipPost.InternshipType = updateDto.InternshipType;

        // Handle image removal
        if (updateDto.RemoveCurrentImage && !string.IsNullOrEmpty(internshipPost.ImageUrl))
        {
            DeleteImage(internshipPost.ImageUrl);
            internshipPost.ImageUrl = null;
        }

        // Handle new image upload
        if (updateDto.NewImage != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(internshipPost.ImageUrl))
                DeleteImage(internshipPost.ImageUrl);
            
            internshipPost.ImageUrl = await SaveImage(updateDto.NewImage, "internship-images");
        }

        _context.Entry(internshipPost).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/internshipposts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInternshipPost(int id)
    {
        var internshipPost = await _context.InternshipPosts.FindAsync(id);
        if (internshipPost == null) return NotFound();

        if (internshipPost.UserId != GetCurrentUserId())
            return Unauthorized();

        // Delete image if exists
        if (!string.IsNullOrEmpty(internshipPost.ImageUrl))
            DeleteImage(internshipPost.ImageUrl);

        _context.InternshipPosts.Remove(internshipPost);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/internshipposts/applied
    [HttpGet("applied")]
    public async Task<ActionResult<IEnumerable<InternshipPostDto>>> GetAppliedInternshipPosts()
    {
        var userId = GetCurrentUserId();
    
        return await _context.Applications
            .Where(a => a.UserId == userId && a.InternshipPostId != null)
            .Include(a => a.InternshipPost)
            .ThenInclude(i => i.User)
            .Include(a => a.InternshipPost)
            .ThenInclude(i => i.Likes)
            .Include(a => a.InternshipPost)
            .ThenInclude(i => i.Comments)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new InternshipPostDto
            {
                Id = a.InternshipPost.Id,
                Title = a.InternshipPost.Title,
                Content = a.InternshipPost.Content,
                Location = a.InternshipPost.Location,
                ExpiryDate = a.InternshipPost.ExpiryDate,
                Type = a.InternshipPost.Type, // Make sure this matches your model
                InternshipType = a.InternshipPost.InternshipType, // Add this if your model has it
                ImageUrl = a.InternshipPost.ImageUrl,
                CreatedAt = a.InternshipPost.CreatedAt,
                User = UserDto.FromUser(a.InternshipPost.User),
                LikeCount = a.InternshipPost.Likes.Count,
                CommentCount = a.InternshipPost.Comments.Count,
                IsLikedByCurrentUser = a.InternshipPost.Likes.Any(l => l.UserId == userId),
                ApplicationStatus = a.Status // Now this will work
            })
            .ToListAsync();
    }

// GET: api/internshipposts/5/applications
    [HttpGet("{id}/applications")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetInternshipPostApplications(int id)
    {
        return await _context.Applications
            .Where(a => a.InternshipPostId == id)
            .Include(a => a.User)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                User = UserDto.FromUser(a.User),
                AppliedAt = a.AppliedAt,
                CvUrl = a.CvUrl,
                Status = a.Status
            })
            .ToListAsync();
    }
    
    // GET: api/internshipposts
    [HttpGet("search")] 
    public async Task<ActionResult<IEnumerable<InternshipPostDto>>> SearchInternshipPosts(
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] InternshipType? internshipType)
    {
        var userId = GetCurrentUserId();
    
        var query = _context.InternshipPosts
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.Title.Contains(search) || i.Content.Contains(search));
        }

        // Apply location filter
        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(i => i.Location.Contains(location));
        }

        // Apply internship type filter
        if (internshipType.HasValue)
        {
            query = query.Where(i => i.InternshipType == internshipType.Value);
        }

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InternshipPostDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Location = i.Location,
                ExpiryDate = i.ExpiryDate,
                Type = i.Type,
                InternshipType = i.InternshipType,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                User = UserDto.FromUser(i.User),
                LikeCount = i.Likes.Count,
                CommentCount = i.Comments.Count,
                IsLikedByCurrentUser = i.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }
    
    [HttpGet("search/applied")]
public async Task<ActionResult<IEnumerable<InternshipPostDto>>> SearchAppliedInternshipPosts(
    [FromQuery] string? search,
    [FromQuery] string? location,
    [FromQuery] InternshipType? internshipType)
{
    var userId = GetCurrentUserId();
    
    var query = _context.Applications
        .Where(a => a.UserId == userId && a.InternshipPostId != null)
        .Include(a => a.InternshipPost)
            .ThenInclude(i => i.User)
        .Include(a => a.InternshipPost)
            .ThenInclude(i => i.Likes)
        .Include(a => a.InternshipPost)
            .ThenInclude(i => i.Comments)
        .AsQueryable();

    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(a => 
            a.InternshipPost.Title.Contains(search) || 
            a.InternshipPost.Content.Contains(search));
    }

    if (!string.IsNullOrEmpty(location))
    {
        query = query.Where(a => a.InternshipPost.Location.Contains(location));
    }

    // Filter by internship type (PFA or PFE)
    if (internshipType.HasValue)
    {
        query = query.Where(a => a.InternshipPost.InternshipType == internshipType.Value);
    }

    return await query
        .OrderByDescending(a => a.AppliedAt)
        .Select(a => new InternshipPostDto
        {
            Id = a.InternshipPost.Id,
            Title = a.InternshipPost.Title,
            Content = a.InternshipPost.Content,
            Location = a.InternshipPost.Location,
            ExpiryDate = a.InternshipPost.ExpiryDate,
            Type = a.InternshipPost.Type,
            InternshipType = a.InternshipPost.InternshipType,
            ImageUrl = a.InternshipPost.ImageUrl,
            CreatedAt = a.InternshipPost.CreatedAt,
            User = UserDto.FromUser(a.InternshipPost.User),
            LikeCount = a.InternshipPost.Likes.Count,
            CommentCount = a.InternshipPost.Comments.Count,
            IsLikedByCurrentUser = a.InternshipPost.Likes.Any(l => l.UserId == userId),
            ApplicationStatus = a.Status
        })
        .ToListAsync();
}


// GET: api/internshipposts/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<InternshipPostDto>>> GetInternshipPostsByUser(int userId)
    {
        var currentUserId = GetCurrentUserId();
    
        return await _context.InternshipPosts
            .Where(i => i.UserId == userId)
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InternshipPostDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Location = i.Location,
                ExpiryDate = i.ExpiryDate,
                Type = i.Type,
                InternshipType = i.InternshipType,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                User = UserDto.FromUser(i.User),
                LikeCount = i.Likes.Count,
                CommentCount = i.Comments.Count,
                IsLikedByCurrentUser = i.Likes.Any(l => l.UserId == currentUserId)
            })
            .ToListAsync();
    }

private int GetCurrentUserId() => 
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

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

    private InternshipPostDto MapToDto(InternshipPost post) => new InternshipPostDto
    {
        Id = post.Id,
        Title = post.Title,
        Content = post.Content,
        Location = post.Location,
        ExpiryDate = post.ExpiryDate,
        Type = post.Type,
        ImageUrl = post.ImageUrl,
        CreatedAt = post.CreatedAt,
        User = UserDto.FromUser(post.User),
        LikeCount = post.Likes?.Count ?? 0,
        CommentCount = post.Comments?.Count ?? 0,
        IsLikedByCurrentUser = post.Likes?.Any(l => l.UserId == GetCurrentUserId()) ?? false
    };
    
    // GET: api/internshipposts/{id}/applications/me
    [HttpGet("{id}/applications/me")]
    public async Task<ActionResult<ApplicationDto>> GetMyApplicationForInternshipPost(int id)
    {
        var userId = GetCurrentUserId();

        var application = await _context.Applications
            .Include(a => a.User)
            .Where(a => a.InternshipPostId == id && a.UserId == userId)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                User = UserDto.FromUser(a.User),
                AppliedAt = a.AppliedAt,
                CvUrl = a.CvUrl,
                Status = a.Status
            })
            .FirstOrDefaultAsync();

        if (application == null)
            return NotFound();

        return application;
    }
    
    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<InternshipPostDto>>> GetMyInternshipPosts()
    {
        var userId = GetCurrentUserId();

        return await _context.InternshipPosts
            .Where(i => i.UserId == userId)
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InternshipPostDto
            {
                Id = i.Id,
                Title = i.Title,
                Content = i.Content,
                Location = i.Location,
                ExpiryDate = i.ExpiryDate,
                Type = i.Type,
                InternshipType = i.InternshipType,
                ImageUrl = i.ImageUrl,
                CreatedAt = i.CreatedAt,
                User = UserDto.FromUser(i.User),
                LikeCount = i.Likes.Count,
                CommentCount = i.Comments.Count,
                IsLikedByCurrentUser = i.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }

}