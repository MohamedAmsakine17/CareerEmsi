using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

[Table("users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }
    
    [Required]
    [MaxLength(150)]
    public string Email { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Password { get; set; }
    
    [AllowNull]
    [MaxLength(255)]
    public string Biography { get; set; }
    
    [AllowNull]
    [MaxLength(255)]
    public string About { get; set; }
    
    [AllowNull]
    [EnumDataType(typeof(Role))]
    public Role Role { get; set; }

    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Student? Student { get; set; }
    public Recruiter? Recruiter { get; set; }
    
    public ICollection<UserSkill>? UserSkills { get; set; }
    
    [MaxLength(255)]
    public string? ProfilePictureUrl { get; set; } 
    
    [MaxLength(255)]
    public string? ProfileBackgroundUrl { get; set; }
    
    public ICollection<Education>? Educations { get; set; }
    public ICollection<Experience>? Experiences { get; set; }
    public ICollection<Connection>? SentConnections { get; set; }
    public ICollection<Connection>? ReceivedConnections { get; set; }
}