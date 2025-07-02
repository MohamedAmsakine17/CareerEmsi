namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

public class PostDto
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto User { get; set; }
    public PostType Type { get; set; }
    public List<string> ImageUrls { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}