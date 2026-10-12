using Microsoft.AspNetCore.Mvc;
using CryptographySolution.DTOs;
using CryptographySolution.Forms;
using CryptographySolution.Models;
using CryptographySolution.Services;

namespace CryptographySolution.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController: ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    // Évidement cette route doit être sécurisé sur un vrais serveur.
    [HttpGet]
    [Route("")]
    public IActionResult GetAll()
    {
        return Ok(_userService.GetAll());
    }

    [HttpGet]
    [Route("{id}")]
    public IActionResult GetOneById(int id)
    {
        UserDTO? user = _userService.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [Route("")]
    public IActionResult UpdateUser(User user)
    {
        return Ok(_userService.AddUser(user));
    }

    [HttpPost]
    [Route("login")]
    public IActionResult LoginUser(LoginForm form)
    {
        return Ok(_userService.LoginUser(form));
    }

    [HttpPut]
    [Route("{id}/edit")]
    public IActionResult UpdateUser(int id, User user)
    {
        UserDTO? updatedUser = _userService.UpdateUser(id, user);

        if (updatedUser == null)
        {
            return NotFound();
        }

        return Ok(updatedUser);
    }
}