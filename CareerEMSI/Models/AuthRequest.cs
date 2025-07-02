using System.ComponentModel.DataAnnotations;

namespace CareerEMSI.Models;

public class AuthRequest
{ 
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; }
    
    
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Biography { get; set; }

    public string About { get; set; }

    public string Role { get; set; }
    
}