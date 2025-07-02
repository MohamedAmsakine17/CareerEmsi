using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class CreateJobPostDto
{
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    public IFormFile? Image { get; set; }
}