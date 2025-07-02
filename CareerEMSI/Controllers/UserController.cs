using CareerEMSI.Models;
using CareerEMSI.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace CareerEMSI.Controllers;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }
    
    //Get: api/User
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] int? limit)
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim == null)
        {
            return Unauthorized(); // Or handle this case properly
        }

        var currentUserId = int.Parse(userIdClaim.Value);

        var query = _context.Users
            .Where(u => u.Id != currentUserId) // exclude yourself
            .Include(u => u.Student)
            .Include(u => u.Recruiter)
            .AsQueryable();

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    
    //Get: api/User/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Recruiter)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null) return NotFound();
        return user;
    }
    
    //Post: api/User
    [HttpPost]
    public async Task<ActionResult<User>> PostUser(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (user.Role == Role.STUDENT && user.Student != null)
        {
            user.Student.UserID = user.Id;
            _context.Students.Add(user.Student);
        } else if (user.Role == Role.RECRUITER && user.Recruiter != null)
        {
            user.Recruiter.UserID = user.Id;
            _context.Recruiters.Add(user.Recruiter);
        }
        
        await _context.SaveChangesAsync();
        return CreatedAtAction("GetUser", new { id = user.Id }, user);
    }
    
    //Put: api/User/5
    [HttpPut("{id}")]
    public async Task<ActionResult<User>> PutUser(int id, User updatedUser){
        if (id != updatedUser.Id) return BadRequest();

        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null) return NotFound();

        existingUser.FirstName = updatedUser.FirstName;
        existingUser.LastName = updatedUser.LastName;
        existingUser.Email = updatedUser.Email;
        existingUser.Biography = updatedUser.Biography;
        existingUser.About = updatedUser.About;
        existingUser.Role = updatedUser.Role;
        existingUser.UpdatedAt = DateTime.UtcNow;
        existingUser.Password = updatedUser.Password;
        existingUser.ProfilePictureUrl = updatedUser.ProfilePictureUrl;
        existingUser.ProfileBackgroundUrl = updatedUser.ProfileBackgroundUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    //Delete: api/User/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<User>> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Recruiter)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null) return NotFound();
        
        if(user.Student != null) _context.Students.Remove(user.Student);
        else if (user.Recruiter != null) _context.Recruiters.Remove(user.Recruiter);
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
    
    // GET: api/users/5/skills
    [HttpGet("{id}/skills")]
    public async Task<ActionResult<IEnumerable<UserSkillDto>>> GetUserSkills(int id)
    {
        var skills = await _context.UserSkills
            .Where(us => us.UserID == id)
            .Include(us => us.Skill)
            .Select(us => new UserSkillDto
            {
                SkillId = us.Skill.SkillID,  // Explicitly access via navigation
                SkillName = us.Skill.Name,
            })
            .ToListAsync();

        return skills;
    }
    
    // POST: api/users/5/skills
    [HttpPost("{id}/skills")]
    public async Task<ActionResult<UserSkill>> AddUserSkill(int id, UserSkill dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists) return NotFound("User not found");
        
        var skillExists = await _context.Skills.AnyAsync(s => s.SkillID == dto.SkillID);
        if (!skillExists) return NotFound("Skill not found");
        
        var userSkill = new UserSkill
        {
            UserID = id,
            SkillID = dto.SkillID,
        };
    
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();
    
        return CreatedAtAction("GetUserSkills", new { id }, userSkill);
    }
    
    // DELETE: api/users/5/skills/3
    [HttpDelete("{userId}/skills/{skillId}")]
    public async Task<IActionResult> RemoveUserSkill(int userId, int skillId)
    {
        var userSkill = await _context.UserSkills
            .FirstOrDefaultAsync(us => us.UserID == userId && us.SkillID == skillId);
    
        if (userSkill == null) return NotFound();
    
        _context.UserSkills.Remove(userSkill);
        await _context.SaveChangesAsync();
    
        return NoContent();
    }
    
    [Authorize] // Require authentication
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSearchResultDto>>> SearchUsers(
        [FromQuery] string keyword, 
        [FromQuery] int limit = 10)
    {
        // Get current user ID from JWT token
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        // Search in first name, last name, or biography (case insensitive)
        var users = await _context.Users
            .Where(u => u.Id != currentUserId) // Exclude yourself
            .Where(u => 
                EF.Functions.Like(u.FirstName, $"%{keyword}%") ||
                EF.Functions.Like(u.LastName, $"%{keyword}%") ||
                EF.Functions.Like(u.Biography, $"%{keyword}%"))
            .Take(limit) // Limit results
            .Select(u => new UserSearchResultDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Biography = u.Biography
            })
            .ToListAsync();

        return Ok(users);
    }
    
    // Modified GET: api/users/students
    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<User>>> GetStudents(
        [FromQuery] string? keyword,
        [FromQuery] int? limit)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var query = _context.Users
            .Where(u => u.Id != currentUserId) // exclude yourself
            .Where(u => u.Role == Role.STUDENT)
            .Include(u => u.Student)
            .ThenInclude(s => s.School)
            .AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(u => 
                EF.Functions.Like(u.FirstName, $"%{keyword}%") ||
                EF.Functions.Like(u.LastName, $"%{keyword}%") ||
                EF.Functions.Like(u.Biography, $"%{keyword}%"));
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

// Modified GET: api/users/recruiters
    [HttpGet("recruiters")]
    public async Task<ActionResult<IEnumerable<User>>> GetRecruiters(
        [FromQuery] string? keyword,
        [FromQuery] int? limit)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
        var query = _context.Users
            .Where(u => u.Id != currentUserId) // exclude yourself
            .Where(u => u.Role == Role.RECRUITER)
            .Include(u => u.Recruiter)
            .ThenInclude(r => r.Company)
            .AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(u => 
                EF.Functions.Like(u.FirstName, $"%{keyword}%") ||
                EF.Functions.Like(u.LastName, $"%{keyword}%") ||
                EF.Functions.Like(u.Biography, $"%{keyword}%"));
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }
    
}