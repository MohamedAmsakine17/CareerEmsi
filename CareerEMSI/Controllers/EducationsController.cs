using CareerEMSI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerEMSI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EducationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EducationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/educations/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Education>>> GetUserEducations(int userId)
    {
        return await _context.Educations
            .Where(e => e.UserId == userId)
            .Include(e => e.School)
            .ToListAsync();
    }

    // POST: api/educations
    [HttpPost]
    public async Task<ActionResult<Education>> PostEducation(Education education)
    {
        _context.Educations.Add(education);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetUserEducations", new { userId = education.UserId }, education);
    }

    // PUT: api/educations/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEducation(int id, Education education)
    {
        if (id != education.Id)
        {
            return BadRequest();
        }

        _context.Entry(education).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EducationExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/educations/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEducation(int id)
    {
        var education = await _context.Educations.FindAsync(id);
        if (education == null)
        {
            return NotFound();
        }

        _context.Educations.Remove(education);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EducationExists(int id)
    {
        return _context.Educations.Any(e => e.Id == id);
    }
}