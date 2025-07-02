using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("Messages")]
public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}