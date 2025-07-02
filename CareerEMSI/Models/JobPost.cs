using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("job_posts")]
public class JobPost : Post
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Location { get; set; }
    
    [Required]
    public DateTime ExpiryDate { get; set; }
    
    [MaxLength(255)]
    public string? ImageUrl { get; set; } // Single image for job posts
}