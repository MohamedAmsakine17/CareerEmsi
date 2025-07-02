using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerEMSI.Models;

[Table("post_images")]
public class PostImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "varchar(255)")]
    public string ImageUrl { get; set; }

    [Required]
    [ForeignKey("Post")]
    public int PostId { get; set; }

    public Post? Post { get; set; }
}