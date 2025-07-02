namespace CareerEMSI.Models;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string FullName {get; set;}
    
    public string Biography { get; set; }
    
    // You can add more properties as needed for your posts
    // But typically for posts you don't need all user details
    public static UserDto FromUser(User user)
    {
        if (user == null) 
            return null;
        
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Biography=user.Biography,
        };
    }
}