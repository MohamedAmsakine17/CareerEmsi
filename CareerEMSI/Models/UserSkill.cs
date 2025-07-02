using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("user_skills")]
public class UserSkill
{
    [Key,Column(Order = 0)]
    public int UserID { get; set; }
    
    [Key, Column(Order = 1)]
    public int SkillID { get; set; }
    
    [ForeignKey("UserID")]
    public User? User { get; set; }
    
    [ForeignKey("SkillID")]
    public Skill? Skill { get; set; }
}