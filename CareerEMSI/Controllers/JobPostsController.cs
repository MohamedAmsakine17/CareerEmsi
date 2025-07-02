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
public class JobPostsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public JobPostsController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: api/jobposts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobPostDto>>> GetJobPosts()
    {
        var userId = GetCurrentUserId();
        
        return await _context.JobPosts
            .Include(j => j.User)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                Location = j.Location,
                ExpiryDate = j.ExpiryDate,
                ImageUrl = j.ImageUrl,
                CreatedAt = j.CreatedAt,
                User = UserDto.FromUser(j.User),
                LikeCount = j.Likes.Count,
                CommentCount = j.Comments.Count,
                IsLikedByCurrentUser = j.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }

    // GET: api/jobposts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<JobPostDto>> GetJobPost(int id)
    {
        var userId = GetCurrentUserId();
        
        var jobPost = await _context.JobPosts
            .Include(j => j.User)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .Where(j => j.Id == id)
            .Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                Location = j.Location,
                ExpiryDate = j.ExpiryDate,
                ImageUrl = j.ImageUrl,
                CreatedAt = j.CreatedAt,
                User = UserDto.FromUser(j.User),
                LikeCount = j.Likes.Count,
                CommentCount = j.Comments.Count,
                IsLikedByCurrentUser = j.Likes.Any(l => l.UserId == userId)
            })
            .FirstOrDefaultAsync();

        if (jobPost == null) return NotFound();

        return jobPost;
    }

    // POST: api/jobposts
    [HttpPost]
    public async Task<ActionResult<JobPostDto>> CreateJobPost([FromForm] CreateJobPostDto createDto)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        
        var jobPost = new JobPost
        {
            Title = createDto.Title,
            Content = createDto.Content,
            Location = createDto.Location,
            ExpiryDate = createDto.ExpiryDate,
            UserId = userId,
            Type = PostType.Job,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        if (createDto.Image != null)
        {
            jobPost.ImageUrl = await SaveImage(createDto.Image, "job-images");
        }

        _context.JobPosts.Add(jobPost);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJobPost), new { id = jobPost.Id }, MapToDto(jobPost));
    }

    // PUT: api/jobposts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJobPost(int id, [FromForm] UpdateJobPostDto updateDto)
    {
        var jobPost = await _context.JobPosts.FindAsync(id);
        if (jobPost == null) return NotFound();

        if (jobPost.UserId != GetCurrentUserId())
            return Unauthorized();

        // Update basic fields
        jobPost.Title = updateDto.Title;
        jobPost.Content = updateDto.Content;
        jobPost.Location = updateDto.Location;
        jobPost.ExpiryDate = updateDto.ExpiryDate;

        // Handle image removal
        if (updateDto.RemoveCurrentImage && !string.IsNullOrEmpty(jobPost.ImageUrl))
        {
            DeleteImage(jobPost.ImageUrl);
            jobPost.ImageUrl = null;
        }

        // Handle new image upload
        if (updateDto.NewImage != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(jobPost.ImageUrl))
                DeleteImage(jobPost.ImageUrl);
            
            jobPost.ImageUrl = await SaveImage(updateDto.NewImage, "job-images");
        }

        _context.Entry(jobPost).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/jobposts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJobPost(int id)
    {
        var jobPost = await _context.JobPosts.FindAsync(id);
        if (jobPost == null) return NotFound();

        if (jobPost.UserId != GetCurrentUserId())
            return Unauthorized();

        // Delete image if exists
        if (!string.IsNullOrEmpty(jobPost.ImageUrl))
            DeleteImage(jobPost.ImageUrl);

        _context.JobPosts.Remove(jobPost);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/jobposts/5/like
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikeJobPost(int id)
    {
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == GetCurrentUserId());

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
        }
        else
        {
            _context.Likes.Add(new Like
            {
                PostId = id,
                UserId = GetCurrentUserId()
            });
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET: api/jobposts/5/comments
    [HttpGet("{id}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetJobPostComments(int id)
    {
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
                User = UserDto.FromUser(c.User),
                LikeCount = c.Likes.Count,
                IsLikedByCurrentUser = c.Likes.Any(l => l.UserId == GetCurrentUserId())
            })
            .ToListAsync();

        return comments;
    }

    // POST: api/jobposts/5/comments
    [HttpPost("{id}/comments")]
    public async Task<ActionResult<CommentDto>> AddJobPostComment(int id, [FromBody] CreateCommentDto commentDto)
    {
        var comment = new Comment
        {
            Content = commentDto.Content,
            UserId = GetCurrentUserId(),
            PostId = id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            User = UserDto.FromUser(await _context.Users.FindAsync(GetCurrentUserId())),
            LikeCount = 0,
            IsLikedByCurrentUser = false
        });
    }
// GET: api/jobposts/applied
    [HttpGet("applied")]
    public async Task<ActionResult<IEnumerable<JobPostDto>>> GetAppliedJobPosts()
    {
        var userId = GetCurrentUserId();
    
        return await _context.Applications
            .Where(a => a.UserId == userId && a.JobPostId != null)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.User)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.Likes)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.Comments)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new JobPostDto
            {
                Id = a.JobPost.Id,
                Title = a.JobPost.Title,
                Content = a.JobPost.Content,
                Location = a.JobPost.Location,
                ExpiryDate = a.JobPost.ExpiryDate,
                Type = a.JobPost.Type,
                ImageUrl = a.JobPost.ImageUrl,
                CreatedAt = a.JobPost.CreatedAt,
                User = UserDto.FromUser(a.JobPost.User),
                LikeCount = a.JobPost.Likes.Count,
                CommentCount = a.JobPost.Comments.Count,
                IsLikedByCurrentUser = a.JobPost.Likes.Any(l => l.UserId == userId),
                ApplicationStatus = a.Status
            })
            .ToListAsync();
    }

// GET: api/jobposts/5/applications
    [HttpGet("{id}/applications")]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetJobPostApplications(int id)
    {
        return await _context.Applications
            .Where(a => a.JobPostId == id)
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
    
    // GET: api/jobposts
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<JobPostDto>>> SearchJobPosts(
        [FromQuery] string? search, 
        [FromQuery] string? location)
    {
        var userId = GetCurrentUserId();
    
        var query = _context.JobPosts
            .Include(j => j.User)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(j => j.Title.Contains(search) || j.Content.Contains(search));
        }

        // Apply location filter
        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(j => j.Location.Contains(location));
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                Location = j.Location,
                ExpiryDate = j.ExpiryDate,
                ImageUrl = j.ImageUrl,
                CreatedAt = j.CreatedAt,
                User = UserDto.FromUser(j.User),
                LikeCount = j.Likes.Count,
                CommentCount = j.Comments.Count,
                IsLikedByCurrentUser = j.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }
    
    // GET: api/jobposts/applied
    [HttpGet("search/applied")]
    public async Task<ActionResult<IEnumerable<JobPostDto>>> SearchAppliedJobPosts(
        [FromQuery] string? search,
        [FromQuery] string? location)
    {
        var userId = GetCurrentUserId();
    
        var query = _context.Applications
            .Where(a => a.UserId == userId && a.JobPostId != null)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.User)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.Likes)
            .Include(a => a.JobPost)
            .ThenInclude(j => j.Comments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a => 
                a.JobPost.Title.Contains(search) || 
                a.JobPost.Content.Contains(search));
        }

        if (!string.IsNullOrEmpty(location))
        {
            query = query.Where(a => a.JobPost.Location.Contains(location));
        }

        return await query
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new JobPostDto
            {
                Id = a.JobPost.Id,
                Title = a.JobPost.Title,
                Content = a.JobPost.Content,
                Location = a.JobPost.Location,
                ExpiryDate = a.JobPost.ExpiryDate,
                Type = a.JobPost.Type,
                ImageUrl = a.JobPost.ImageUrl,
                CreatedAt = a.JobPost.CreatedAt,
                User = UserDto.FromUser(a.JobPost.User),
                LikeCount = a.JobPost.Likes.Count,
                CommentCount = a.JobPost.Comments.Count,
                IsLikedByCurrentUser = a.JobPost.Likes.Any(l => l.UserId == userId),
                ApplicationStatus = a.Status
            })
            .ToListAsync();
    }
    
    // GET: api/jobposts/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<JobPostDto>>> GetJobPostsByUser(int userId)
    {
        var currentUserId = GetCurrentUserId();
    
        return await _context.JobPosts
            .Where(j => j.UserId == userId)
            .Include(j => j.User)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                Location = j.Location,
                ExpiryDate = j.ExpiryDate,
                ImageUrl = j.ImageUrl,
                CreatedAt = j.CreatedAt,
                User = UserDto.FromUser(j.User),
                LikeCount = j.Likes.Count,
                CommentCount = j.Comments.Count,
                IsLikedByCurrentUser = j.Likes.Any(l => l.UserId == currentUserId)
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

    private JobPostDto MapToDto(JobPost post) => new JobPostDto
    {
        Id = post.Id,
        Title = post.Title,
        Content = post.Content,
        Location = post.Location,
        ExpiryDate = post.ExpiryDate,
        ImageUrl = post.ImageUrl,
        CreatedAt = post.CreatedAt,
        User = UserDto.FromUser(post.User),
        LikeCount = post.Likes?.Count ?? 0,
        CommentCount = post.Comments?.Count ?? 0,
        IsLikedByCurrentUser = post.Likes?.Any(l => l.UserId == GetCurrentUserId()) ?? false
    };
    
    // GET: api/jobposts/{id}/applications/me
    [HttpGet("{id}/applications/me")]
    public async Task<ActionResult<ApplicationDto>> GetMyApplicationForJobPost(int id)
    {
        var userId = GetCurrentUserId();

        var application = await _context.Applications
            .Include(a => a.User)
            .Where(a => a.JobPostId == id && a.UserId == userId)
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
    public async Task<ActionResult<IEnumerable<JobPostDto>>> GetMyJobPosts()
    {
        var userId = GetCurrentUserId();

        return await _context.JobPosts
            .Where(j => j.UserId == userId)
            .Include(j => j.User)
            .Include(j => j.Likes)
            .Include(j => j.Comments)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JobPostDto
            {
                Id = j.Id,
                Title = j.Title,
                Content = j.Content,
                Location = j.Location,
                ExpiryDate = j.ExpiryDate,
                ImageUrl = j.ImageUrl,
                CreatedAt = j.CreatedAt,
                User = UserDto.FromUser(j.User),
                LikeCount = j.Likes.Count,
                CommentCount = j.Comments.Count,
                IsLikedByCurrentUser = j.Likes.Any(l => l.UserId == userId)
            })
            .ToListAsync();
    }

}