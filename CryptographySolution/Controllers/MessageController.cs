using Microsoft.AspNetCore.Mvc;
using CryptographySolution.Forms;
using CryptographySolution.Services;

namespace CryptographySolution.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController: ControllerBase
{
    private readonly MessageService _messageService;

    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet]
    [Route("")]
    public IActionResult GetAll()
    {
        return Ok(_messageService.GetAll());
    }

    [HttpGet]
    [Route("{userId}")]
    public IActionResult GetAllUserMessage(int userId)
    {
        return Ok(_messageService.GetAllUserMessage(userId));
    }

    [HttpPost]
    [Route("")]
    public IActionResult SendMessage(SendMessageForm form)
    {
        return Ok(_messageService.SendMessage(form));
    }
}