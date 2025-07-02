using CareerEMSI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerEMSI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExperiencesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExperiencesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/experiences/user/5
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Experience>>> GetUserExperiences(int userId)
    {
        return await _context.Experiences
            .Where(e => e.UserId == userId)
            .Include(e => e.Company)
            .ToListAsync();
    }

    // GET: api/experiences/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Experience>> GetExperience(int id)
    {
        var experience = await _context.Experiences
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (experience == null)
        {
            return NotFound();
        }

        return experience;
    }

    // POST: api/experiences
    [HttpPost]
    public async Task<ActionResult<Experience>> PostExperience(Experience experience)
    {
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetExperience", new { id = experience.Id }, experience);
    }

    // PUT: api/experiences/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutExperience(int id, Experience experience)
    {
        if (id != experience.Id)
        {
            return BadRequest();
        }

        _context.Entry(experience).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ExperienceExists(id))
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

    // DELETE: api/experiences/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExperience(int id)
    {
        var experience = await _context.Experiences.FindAsync(id);
        if (experience == null)
        {
            return NotFound();
        }

        _context.Experiences.Remove(experience);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ExperienceExists(int id)
    {
        return _context.Experiences.Any(e => e.Id == id);
    }
}