using System.ComponentModel.DataAnnotations;

namespace CryptographySolution.Forms;

public class LoginForm
{
    [Required]
    public string username { get; set; }
    [Required]
    public string password { get; set; }
}
