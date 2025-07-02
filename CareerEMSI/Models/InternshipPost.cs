using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

[Table("internship_posts")]
public class InternshipPost : Post
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Location { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    [Required]
    public InternshipType InternshipType  { get; set; } // PFA or PFE
    
    [MaxLength(255)]
    public string? ImageUrl { get; set; } // Single image for internship posts
    
    public InternshipPost()
    {
        this.Type = PostType.Internship; // Set base property directly
    }
}