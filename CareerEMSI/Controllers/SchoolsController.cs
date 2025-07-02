using CareerEMSI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerEMSI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchoolsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SchoolsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/schools
    [HttpGet]
    public async Task<ActionResult<IEnumerable<School>>> GetSchools()
    {
        return await _context.Schools.ToListAsync();
    }

    // GET: api/schools/5
    [HttpGet("{id}")]
    public async Task<ActionResult<School>> GetSchool(int id)
    {
        var school = await _context.Schools.FindAsync(id);
        if (school == null) return NotFound();
        return school;
    }

    // POST: api/schools
    [HttpPost]
    public async Task<ActionResult<School>> PostSchool([FromBody] School school)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        school.Id = 0; 

        _context.Schools.Add(school);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSchool", new { id = school.Id }, school);
    }

    // PUT: api/schools/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSchool(int id, School school)
    {
        if (id != school.Id) return BadRequest();
        
        _context.Entry(school).State = EntityState.Modified;
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SchoolExists(id)) return NotFound();
            else throw;
        }
        
        return NoContent();
    }

    // DELETE: api/schools/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchool(int id)
    {
        var school = await _context.Schools.FindAsync(id);
        if (school == null) return NotFound();
        
        _context.Schools.Remove(school);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    private bool SchoolExists(int id)
    {
        return _context.Schools.Any(e => e.Id == id);
    }
    
    // POST: api/schools/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<School>>> PostSchools([FromBody] List<School> schools)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Schools.AddRange(schools);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetSchools", schools);
    }
    
    [HttpPost("{id}/logo")]
    public async Task<IActionResult> UploadLogo(int id, IFormFile file)
    {
        var school = await _context.Schools.FindAsync(id);
        if (school == null) return NotFound();

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        // Validate file type and size
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Invalid file type. Only images are allowed");

        if (file.Length > 5 * 1024 * 1024) // 5MB max
            return BadRequest("File size exceeds 5MB limit");

        // Create unique filename
        var fileName = $"{Guid.NewGuid()}{extension}";
        var uploadsFolder = Path.Combine("wwwroot","uploads", "SchoolLogos");
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update school record
        school.LogoUrl = $"/uploads/SchoolLogos/{fileName}";
        await _context.SaveChangesAsync();

        return Ok(new { LogoUrl = school.LogoUrl });
    }
}