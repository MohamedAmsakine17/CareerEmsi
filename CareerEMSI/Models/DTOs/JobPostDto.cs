using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

public class JobPostDto : PostDto
{
    public string Title { get; set; }
    public string Location { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? ImageUrl { get; set; }
    public ApplicationStatus? ApplicationStatus { get; set; } // Add this line

}