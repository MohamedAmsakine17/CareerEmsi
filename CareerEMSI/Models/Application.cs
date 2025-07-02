using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

[Table(("applications"))]
public class Application
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
        
    [Required]
    public int UserId { get; set; }
    public User User { get; set; }
        
    public int? JobPostId { get; set; }
    public JobPost? JobPost { get; set; }
        
    public int? InternshipPostId { get; set; }
    public InternshipPost? InternshipPost { get; set; }
        
    [Required]
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        
    public string CvUrl { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
}