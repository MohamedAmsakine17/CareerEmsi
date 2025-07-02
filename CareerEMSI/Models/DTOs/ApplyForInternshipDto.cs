using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class ApplyForInternshipDto
{
    [Required]
    public int InternshipPostId { get; set; }
    
    [Required]
    public IFormFile CvFile { get; set; }
}