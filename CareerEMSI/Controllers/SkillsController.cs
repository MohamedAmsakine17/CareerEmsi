using CareerEMSI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerEMSI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SkillsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SkillsController(AppDbContext context)
    {
        _context = context;
    }
    
    // Get: api/skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        return await _context.Skills.ToListAsync();
    }
    
    // Get: api/skills/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null) return NotFound();
        return skill;
    }
    
    // POST: api/skills
    [HttpPost]
    public async Task<ActionResult<Skill>> PostSkill(Skill skill)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return CreatedAtAction("GetSkill", new { id = skill.SkillID }, skill);
    }
    
    // DELETE: api/skills/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null) return NotFound();
        
        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Skill>>> SearchSkills(string query)
    {
        var skills = await _context.Skills
            .Where(s => s.Name.Contains(query))
            .ToListAsync();

        return Ok(skills);
    }
}