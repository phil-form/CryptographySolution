using CryptographySolution.DTOs;
using CryptographySolution.Models;
using CryptographySolution.Services;

namespace CryptographySolution.Mappers;

public class MessageMapper
{
    private readonly CryptographyService _cryptographyService;

    public MessageMapper(CryptographyService cryptographyService)
    {
        _cryptographyService = cryptographyService;
    }

    public MessageDTO MapToDTO(Message message, User? receiver)
    {
        return new MessageDTO
        {
            id = message.id,
            senderId = message.senderId,
            receiverId = message.receiverId,
            message = receiver != null ?
                _cryptographyService.DecryptAes(message.message)
                : message.message,
        };
    }
}