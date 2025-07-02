using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("schools")]
public class School
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(200)]
    public string WebsiteURL { get; set; }
    
    [MaxLength(255)]
    public string? LogoUrl { get; set; } 
    
    [MaxLength(500)]
    public string Description { get; set; }
    
    [MaxLength(50)]
    public string SchoolType { get; set; } 
    
    public ICollection<Student>? Students { get; set; }
}