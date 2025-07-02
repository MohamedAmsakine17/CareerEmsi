using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class CreateApplicationDto
{
    [Required]
    public int? JobPostId { get; set; }
        
    [Required]
    public int? InternshipPostId { get; set; }
        
    [Required]
    public IFormFile CvFile { get; set; }
}