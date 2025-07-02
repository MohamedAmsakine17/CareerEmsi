// Connection.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerEMSI.Models.Enums;


namespace CareerEMSI.Models;

[Table("connections")]
public class Connection
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ConnectionId { get; set; }

    [Required]
    public int RequesterId { get; set; }

    [Required]
    public int ReceiverId { get; set; }

    [Required]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RequesterId")]
    public virtual User Requester { get; set; }

    [ForeignKey("ReceiverId")]
    public virtual User Receiver { get; set; }
}
