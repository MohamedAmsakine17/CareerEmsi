namespace CareerEMSI.Models;

public class ProfileImageUpdateDto
{
    public IFormFile? ProfilePicture { get; set; }
    public IFormFile? ProfileBackground { get; set; }
}