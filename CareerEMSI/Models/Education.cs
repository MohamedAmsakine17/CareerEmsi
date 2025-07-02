using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("educations")]
public class Education
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [ForeignKey("School")]
    public int SchoolId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Degree { get; set; } // e.g., "Bachelor's", "Master's"

    [Required]
    [MaxLength(100)]
    public string FieldOfStudy { get; set; } // e.g., "Computer Science"

    [Required]
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndDate { get; set; } // Nullable for current education

    // Navigation properties
    public User? User { get; set; }
    public School? School { get; set; }
}