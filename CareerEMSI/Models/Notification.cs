using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

[Table("notifications")]
public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; } // Recipient ID
    
    public User User { get; set; }
    
    [Required]
    public string Message { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int? RelatedEntityId { get; set; } 
    
    [MaxLength(50)]
    public string? SenderName { get; set; } 
    
    public string? SenderImageUrl { get; set; }
    
    [Column(TypeName = "tinyint")]
    public PostType? PostType { get; set; }
}