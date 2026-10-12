using System.Security.Cryptography;
using BStorm.Tools.Encryptions.Cryptography.Asymmetric;
using Microsoft.EntityFrameworkCore;
using CryptographySolution.DTOs;
using CryptographySolution.Forms;
using CryptographySolution.Mappers;
using CryptographySolution.Models;

namespace CryptographySolution.Services;

public class UserService
{
    private CryptographyService _cryptographyService;
    private DemoDbContext _dbContext;
    private UserMapper _userMapper;

    public UserService(CryptographyService cryptographyService, DemoDbContext dbContext, UserMapper userMapper)
    {
        _cryptographyService = cryptographyService;
        _dbContext = dbContext;
        _userMapper = userMapper;
    }

    public IEnumerable<UserDTO> GetAll()
    {
        IEnumerable<UserDTO> users = _dbContext.Users
            .ToList()
            .Select(u => _userMapper.MapToDTO(u));

        return users;
    }

    public UserDTO? GetById(int id)
    {
        User? outUser = _dbContext.Users.Find(id);

        if (outUser == null)
        {
            return null;
        }

        return _userMapper.MapToDTO(outUser);
    }

    public UserDTO? LoginUser(LoginForm form)
    {
        User outUser = _dbContext
            .Users
            .Where(u => u.username == form.username)
            .FirstOrDefault();

        if (outUser == null || !_cryptographyService.VerifyPassword(form.password, outUser.password))
        {
            return null;
        }

        // Le mot de passe en clair n'est disponible qu'ici : on en profite pour migrer
        // les hash BCrypt (ou Argon2id avec d'anciens paramètres) vers l'Argon2id actuel.
        if (_cryptographyService.NeedsRehash(outUser.password))
        {
            outUser.password = _cryptographyService.HashPassword(form.password);
            _dbContext.SaveChanges();
        }

        return _userMapper.MapToDTO(outUser, true);
    }

    public UserDTO AddUser(User user)
    {
        user.password = _cryptographyService.HashPassword(user.password);
        user.secretMessage = this._cryptographyService.EncryptAes(user.secretMessage);
        RsaEncryption rsa = new RsaEncryption();
        // Pour bien faire, on devrait utiliser une private cryptée par un mot de passe.
        user.privateKey = rsa.ToPem(true);
        user.publicKey = rsa.ToPem(false);

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();

        return _userMapper.MapToDTO(user);
    }

    public UserDTO? UpdateUser(int id, User user)
    {
        User? u = _dbContext.Users.Find(id);

        if (u == null)
        {
            return null;
        }

        // On modifie l'entité déjà suivie par EF plutôt que d'en attacher une seconde avec le même id.
        // Les clés RSA restent inchangées pour pouvoir toujours déchiffrer les messages reçus.
        u.username = user.username;
        u.password = _cryptographyService.HashPassword(user.password);
        u.secretMessage = this._cryptographyService.EncryptAes(user.secretMessage);
        _dbContext.SaveChanges();

        return _userMapper.MapToDTO(u);
    }
}