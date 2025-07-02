using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class ApplyForJobDto
{  [Required]
    public int JobPostId { get; set; }
    
    [Required]
    public IFormFile CvFile { get; set; }
    
}