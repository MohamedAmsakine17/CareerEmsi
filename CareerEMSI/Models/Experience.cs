using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("experiences")]
public class Experience
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [ForeignKey("Company")]
    public int CompanyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } // e.g., "Software Engineer", "Marketing Manager"

    [Required]
    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndDate { get; set; } // Nullable for current position

    [MaxLength(500)]
    public string Description { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Company? Company { get; set; }
}