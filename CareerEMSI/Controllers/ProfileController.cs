using System.Security.Claims;
using CareerEMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerEMSI.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }
    
    // Get: api/profile/me
    [HttpGet("me")]
    public async Task<ActionResult<User>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        var user = await _context.Users
            .Include(u=> u.Student)
            .ThenInclude(s => s.School)
            .Include(u => u.Recruiter)
            .ThenInclude(r => r.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if(user == null) return NotFound();

        return user;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetPublicProfile(int id)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .ThenInclude(s => s.School)
            .Include(u => u.Recruiter)
            .ThenInclude(r => r.Company)
            .Include(u => u.Educations)
            .ThenInclude(e => e.School)
            .Include(u => u.Experiences)
            .ThenInclude(e => e.Company)
            .Include(u => u.UserSkills)
            .ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(u => u.Id == id);
    
        if (user == null) return NotFound();

        var profile = new
        {
            // Basic Info
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.About,
            user.Role,
            user.Biography,
            user.ProfilePictureUrl,
            user.ProfileBackgroundUrl,
        
            // Education/Work Info
            School = user.Student?.School,
            Company = user.Recruiter?.Company,
        
            // Educations
            Educations = user.Educations?.Select(e => new 
            {
                e.Id,
                School = e.School,
                e.Degree,
                e.FieldOfStudy,
                e.StartDate,
                e.EndDate
            }),
        
            // Experiences
            Experiences = user.Experiences?.Select(e => new 
            {
                e.Id,
                Company = e.Company,
                e.Title,
                e.StartDate,
                e.EndDate,
                e.Description
            }),
        
            // Skills
            Skills = user.UserSkills?.Select(us => new 
            {
                us.Skill.SkillID,
                us.Skill.Name,
            })
        };

        return Ok(profile);
    }
    
    [HttpPut("me/images")]
    [Authorize]
    public async Task<IActionResult> UpdateProfileImages([FromForm] ProfileImageUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var user = await _context.Users.FindAsync(userId);
    
        if (user == null) return NotFound();
    
        // Handle profile picture
        if (dto.ProfilePicture != null)
        {
            user.ProfilePictureUrl = await SaveImage(dto.ProfilePicture, "profile-pictures");
        }
    
        // Handle background image
        if (dto.ProfileBackground != null)
        {
            user.ProfileBackgroundUrl = await SaveImage(dto.ProfileBackground, "profile-backgrounds");
        }
    
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    
        return Ok(new {
            ProfilePictureUrl = user.ProfilePictureUrl,
            ProfileBackgroundUrl = user.ProfileBackgroundUrl
        });
    }

    private async Task<string> SaveImage(IFormFile imageFile, string folderName)
    {
        // Create unique filename
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
        var folderPath = Path.Combine("wwwroot", "uploads", folderName);
        var filePath = Path.Combine(folderPath, fileName);
    
        // Ensure directory exists
        Directory.CreateDirectory(folderPath);
    
        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }
    
        // Return relative URL
        return $"/uploads/{folderName}/{fileName}";
    }
}