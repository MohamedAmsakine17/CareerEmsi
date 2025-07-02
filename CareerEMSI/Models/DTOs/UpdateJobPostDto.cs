using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class UpdateJobPostDto
{
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    public IFormFile? NewImage { get; set; }  // Single image replacement
    public bool RemoveCurrentImage { get; set; } = false;
}