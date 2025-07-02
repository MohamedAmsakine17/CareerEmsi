using Microsoft.AspNetCore.SignalR;

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
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IHubContext<NotificationHub> _hubContext;


    public ApplicationsController(AppDbContext context, IWebHostEnvironment env, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
        _env = env;
    }

    // GET: api/applications (gets current user's applications)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationDto>>> GetApplications()
    {
        var userId = GetCurrentUserId();

        return await _context.Applications
            .Include(a => a.User)
            .Include(a => a.JobPost)
            .Include(a => a.InternshipPost)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                User = UserDto.FromUser(a.User),
                JobPostId = a.JobPostId,
                InternshipPostId = a.InternshipPostId,
                AppliedAt = a.AppliedAt,
                CvUrl = a.CvUrl,
                Status = a.Status,
            })
            .ToListAsync();
    }

    // GET: api/applications/5 (gets specific application if owned by user)
    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationDto>> GetApplication(int id)
    {
        var userId = GetCurrentUserId();

        var application = await _context.Applications
            .Include(a => a.User)
            .Include(a => a.JobPost)
            .Include(a => a.InternshipPost)
            .Where(a => a.Id == id && a.UserId == userId)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                User = UserDto.FromUser(a.User),
                JobPostId = a.JobPostId,
                InternshipPostId = a.InternshipPostId,
                AppliedAt = a.AppliedAt,
                CvUrl = a.CvUrl,
                Status = a.Status,
            })
            .FirstOrDefaultAsync();

        if (application == null) return NotFound();

        return application;
    }

    // POST: api/applications/job (apply for a job)
    [HttpPost("job")]
    public async Task<ActionResult<ApplicationDto>> ApplyForJob([FromForm] ApplyForJobDto applyDto)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        // Check if job exists
        var jobPost = await _context.JobPosts
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == applyDto.JobPostId);
        
        if (jobPost ==null)
            return NotFound("Job post not found");
        

        // Check if user already applied
        var existingApplication = await _context.Applications
            .FirstOrDefaultAsync(a => a.UserId == userId && a.JobPostId == applyDto.JobPostId);

        if (existingApplication != null)
        {
            Console.WriteLine("User already applied to this job"); 
            return BadRequest("You have already applied to this job");
        }

        Console.WriteLine("Saving CV file...");
        var cvUrl = await SaveCv(applyDto.CvFile);

        var application = new Application
        {
            UserId = userId,
            JobPostId = applyDto.JobPostId,
            InternshipPostId = null,
            CvUrl = cvUrl,
            AppliedAt = DateTime.UtcNow,
            Status = ApplicationStatus.Pending
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        
        var notification = new Notification
        {
            UserId = jobPost.UserId,
            Message = $"applied to your job: {jobPost.Title}",
            Type = NotificationType.NewApplication,
            RelatedEntityId = application.Id,
            SenderName = $"{user.FirstName} {user.LastName}",
            SenderImageUrl = user.ProfilePictureUrl
        };
    
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.Group($"user-{jobPost.UserId}")
            .SendAsync("ReceiveNotification", new {
                id = notification.Id,
                message = notification.Message,
                type = notification.Type.ToString(),
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                relatedEntityId = notification.RelatedEntityId,
                senderName = notification.SenderName,
                senderImageUrl = notification.SenderImageUrl,
                // Additional application data
                applicationId = application.Id,
                postTitle = jobPost.Title,
                postType = "Job"
            });
        
        Console.WriteLine($"Application created with ID {application.Id}");
        return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, MapToDto(application));
    }

    // POST: api/applications/internship (apply for an internship)
    [HttpPost("internship")]
    public async Task<ActionResult<ApplicationDto>> ApplyForInternship([FromForm] ApplyForInternshipDto applyDto)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        // Check if internship exists
        var internshipPost = await _context.InternshipPosts
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == applyDto.InternshipPostId);
        
        if (internshipPost == null) return NotFound("Internship post not found");

        // Check if user already applied
        var existingApplication = await _context.Applications
            .FirstOrDefaultAsync(a => a.UserId == userId && a.InternshipPostId == applyDto.InternshipPostId);

        if (existingApplication != null)
        {
            return BadRequest("You have already applied to this internship");
        }

        // Save CV file
        var cvUrl = await SaveCv(applyDto.CvFile);

        var application = new Application
        {
            UserId = userId,
            InternshipPostId = applyDto.InternshipPostId,
            CvUrl = cvUrl,
            JobPostId = null,
            AppliedAt = DateTime.UtcNow,
            Status = ApplicationStatus.Pending
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        var notification = new Notification
        {
            UserId = internshipPost.UserId,
            Message = $"applied to your internship: {internshipPost.Title}",
            Type = NotificationType.NewApplication,
            RelatedEntityId = application.Id,
            SenderName = $"{user.FirstName} {user.LastName}",
            SenderImageUrl = user.ProfilePictureUrl
        };
    
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    
        await _hubContext.Clients.Group($"user-{internshipPost.UserId}")
            .SendAsync("ReceiveNotification", new {
                id = notification.Id,
                message = notification.Message,
                type = notification.Type.ToString(),
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                relatedEntityId = notification.RelatedEntityId,
                senderName = notification.SenderName,
                senderImageUrl = notification.SenderImageUrl,
                // Additional application data
                applicationId = application.Id,
                postTitle = internshipPost.Title,
                postType = "Internship"
            });
        
        return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, MapToDto(application));
    }

    // DELETE: api/applications/5 (delete user's own application)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var userId = GetCurrentUserId();

        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (application == null) return NotFound();

        // Delete CV file
        if (!string.IsNullOrEmpty(application.CvUrl))
        {
            DeleteFile(application.CvUrl);
        }

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/applications/5/status (update application status - for employers)
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] UpdateApplicationStatusDto updateDto)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application == null) return NotFound();

        // Here you should add authorization to check if the current user is the post owner
        // For example:
        // var isOwner = await _context.JobPosts/InternshipPosts.AnyAsync(p => p.Id == application.JobPostId/InternshipPostId && p.UserId == currentUserId);
        // if (!isOwner) return Unauthorized();

        application.Status = updateDto.Status;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> SaveCv(IFormFile cvFile)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(cvFile.FileName);
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", "cvs");
        var filePath = Path.Combine(folderPath, fileName);

        Directory.CreateDirectory(folderPath);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await cvFile.CopyToAsync(stream);
        }

        return $"/uploads/cvs/{fileName}";
    }

    private void DeleteFile(string fileUrl)
    {
        var filePath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private int GetCurrentUserId() => 
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    private ApplicationDto MapToDto(Application application) => new ApplicationDto
    {
        Id = application.Id,
        User = UserDto.FromUser(application.User),
        JobPostId = application.JobPostId,
        InternshipPostId = application.InternshipPostId,
        AppliedAt = application.AppliedAt,
        CvUrl = application.CvUrl,
        Status = application.Status,
    };
}