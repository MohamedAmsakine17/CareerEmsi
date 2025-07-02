using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;
using CareerEMSI.Models.Enums;

[Table("posts")]
public class Post
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
    [Column(TypeName = "tinyint")]
    public PostType Type { get; set; } = PostType.Public;

    public ICollection<PostImage>? Images { get; set; }
    public ICollection<Like>? Likes { get; set; }
    public ICollection<Comment>? Comments { get; set; }
}