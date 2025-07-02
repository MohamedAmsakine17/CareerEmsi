namespace CareerEMSI.Models;

using CareerEMSI.Models.Enums;

public class CreatePostDto
{
    public string Content { get; set; }
    public PostType Type { get; set; } = PostType.Public;
    public List<IFormFile>? Images { get; set; }
}