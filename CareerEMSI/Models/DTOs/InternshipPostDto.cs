namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

public class InternshipPostDto : PostDto
{
    public string Title { get; set; }
    public string Location { get; set; }
    public DateTime ExpiryDate { get; set; }
    public PostType Type{get;set;}
    public InternshipType InternshipType { get; set; }
    public string? ImageUrl { get; set; }
    public ApplicationStatus? ApplicationStatus { get; set; } // Add this line
    
}