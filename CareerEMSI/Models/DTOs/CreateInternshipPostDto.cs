using System.ComponentModel.DataAnnotations;
using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

public class CreateInternshipPostDto
{
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    [Required]
    public InternshipType InternshipType { get; set; }
    
    public IFormFile? Image { get; set; }
}