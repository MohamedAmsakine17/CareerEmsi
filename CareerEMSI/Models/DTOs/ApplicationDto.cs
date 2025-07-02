namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

public class ApplicationDto
{  public int Id { get; set; }
    public UserDto User { get; set; }
    public int? JobPostId { get; set; }
    public int? InternshipPostId { get; set; }
    public DateTime AppliedAt { get; set; }
    public string CvUrl { get; set; }
    public ApplicationStatus Status { get; set; }
    
}