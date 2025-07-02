using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CareerEMSI.Models;

[Table("students")]
public class Student
{
    [Key,ForeignKey("User")]
    public int UserID { get; set; }
    
    [ForeignKey("School")]
    public int SchoolId { get; set; }
    
    public User User { get; set; }
    public School School { get; set; }
}