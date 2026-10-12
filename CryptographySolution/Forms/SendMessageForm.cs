using System.ComponentModel.DataAnnotations;

namespace CryptographySolution.Forms;

public class SendMessageForm
{
    [Required]
    public string message { get; set; }
    [Required]
    public int senderId { get; set; }
    [Required]
    public int receiverId { get; set; }
}