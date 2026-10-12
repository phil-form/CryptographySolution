namespace CryptographySolution.DTOs;

public class UserDTO
{
    public int id { get; set; }
    public string username { get; set; }
    public string secretMessage { get; set; }
    public string publicKey { get; set; }
}