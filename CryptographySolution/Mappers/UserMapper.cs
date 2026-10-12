using CryptographySolution.DTOs;
using CryptographySolution.Models;
using CryptographySolution.Services;

namespace CryptographySolution.Mappers;

public class UserMapper
{
    private readonly CryptographyService _cryptographyService;

    public UserMapper(CryptographyService cryptographyService)
    {
        _cryptographyService = cryptographyService;
    }

    public UserDTO MapToDTO(User user, bool decryptSecretMessage = false)

    {
        return new UserDTO
        {
            id = user.id,
            username = user.username,
            secretMessage = decryptSecretMessage ? _cryptographyService.DecryptAes(user.secretMessage) : user.secretMessage,
            publicKey = user.publicKey
        };
    }
}