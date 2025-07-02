using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("recruiters")]
public class Recruiter
{
    [Key,ForeignKey("User")]
    public int UserID { get; set; }
    
    [ForeignKey("Company")]
    public int CompanyId { get; set; }
    
    public User User { get; set; }
    public Company Company { get; set; }
}