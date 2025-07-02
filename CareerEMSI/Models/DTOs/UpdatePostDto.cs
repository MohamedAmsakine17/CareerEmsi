using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

public class UpdatePostDto
{
    [Required]
    public string Content { get; set; }
    
    public PostType Type { get; set; } = PostType.Public;
    
    public List<IFormFile>? NewImages { get; set; }  // For adding new images
    public List<int>? ImageIdsToDelete { get; set; } // For removing existing images
}