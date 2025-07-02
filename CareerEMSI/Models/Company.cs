using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("company")]
public class Company
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    
    [MaxLength(150)]
    public string Industry { get; set; }
    
    [MaxLength(200)]
    public string WebsiteURL { get; set; }
    
    public ICollection<Recruiter>? Recruiters { get; set; }
}