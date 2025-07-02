using System.ComponentModel.DataAnnotations;
using CareerEMSI.Models.Enums;

namespace CareerEMSI.Models;

public class UpdateApplicationStatusDto
{
    [Required]
    public ApplicationStatus Status { get; set; }
}