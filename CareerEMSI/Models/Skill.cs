using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("skills")]
public class Skill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SkillID { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    public ICollection<UserSkill> UserSkills { get; set; }
}