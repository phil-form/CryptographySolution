using CryptographySolution.DTOs;
using CryptographySolution.Forms;
using CryptographySolution.Mappers;
using CryptographySolution.Models;

namespace CryptographySolution.Services;

public class MessageService
{
    private CryptographyService _cryptographyService;
    private DemoDbContext _dbContext;
    private MessageMapper _messageMapper;

    public MessageService(CryptographyService cryptographyService, DemoDbContext dbContext, MessageMapper messageMapper)
    {
        _cryptographyService = cryptographyService;
        _dbContext = dbContext;
        _messageMapper = messageMapper;
    }

    public IEnumerable<MessageDTO> GetAll()
    {
        IEnumerable<MessageDTO> messages = _dbContext.Messages
            .ToList()
            .Select(m => _messageMapper.MapToDTO(m, null));

        return messages;
    }

    public IEnumerable<MessageDTO> GetAllUserMessage(int userId)
    {
        User user = _dbContext.Users.Find(userId);
        IEnumerable<MessageDTO> messages = _dbContext.Messages.Where(m => m.receiverId == userId)
            .ToList()
            .Select(m => _messageMapper.MapToDTO(m, user));

        return messages;
    }

    public MessageDTO SendMessage(SendMessageForm form)
    {
        Message message = new Message();
        // Chiffré en AES avec la clé AesKey de Secrets/secrets.bin.
        message.message = this._cryptographyService.EncryptAes(form.message);
        message.senderId = form.senderId;
        message.receiverId = form.receiverId;

        _dbContext.Messages.Add(message);
        _dbContext.SaveChanges();

        return _messageMapper.MapToDTO(message, null);
    }
}