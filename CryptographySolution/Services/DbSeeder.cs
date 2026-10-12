using Microsoft.EntityFrameworkCore;
using CryptographySolution.Models;

namespace CryptographySolution.Services;

// Applique les migrations en attente puis ajoute des utilisateurs de démonstration si la table Users est vide.
// Passe par UserService.AddUser : mots de passe hashés en Argon2id, secretMessage chiffré en AES, clés RSA générées.
public static class DbSeeder
{
    private static readonly (string username, string password, string secretMessage)[] Users =
    {
        ("alice", "alice1234", "Le code du coffre est 4242"),
        ("bob", "bob1234", "J'ai caché les clés sous le paillasson"),
        ("charlie", "charlie1234", "Mon plat préféré est la raclette")
    };

    public static void Seed(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        DemoDbContext dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
        dbContext.Database.Migrate();

        if (dbContext.Users.Any())
        {
            return;
        }

        UserService userService = scope.ServiceProvider.GetRequiredService<UserService>();

        foreach ((string username, string password, string secretMessage) in Users)
        {
            userService.AddUser(new User
            {
                username = username,
                password = password,
                secretMessage = secretMessage
            });
        }
    }
}
