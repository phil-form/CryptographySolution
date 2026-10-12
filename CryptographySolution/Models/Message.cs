using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptographySolution.Models;

public record Message
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id { get; set; }
    [Required]
    public string message { get; set; }
    public int senderId { get; set; }
    public int receiverId { get; set; }
    [ForeignKey("senderId")]
    public User sender { get; set; }
    [ForeignKey("receiverId")]
    public User receiver { get; set; }
}