using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class CreateCommentDto
{
    [Required]
    [MaxLength(500)]
    public string Content { get; set; }
}