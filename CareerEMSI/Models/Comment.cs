using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("comments")]
public class Comment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string Content { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [ForeignKey("Post")]
    public int PostId { get; set; }

    public Post? Post { get; set; }

    public ICollection<CommentLike>? Likes { get; set; }
}