namespace CryptographySolution.DTOs;

public class MessageDTO
{
    public int id { get; set; }
    public string message { get; set; }
    public int senderId { get; set; }
    public int receiverId { get; set; }
}