using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("comment_likes")]
public class CommentLike
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [ForeignKey("Comment")]
    public int CommentId { get; set; }

    public Comment? Comment { get; set; }
}