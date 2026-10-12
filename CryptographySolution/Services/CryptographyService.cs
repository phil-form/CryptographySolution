using System.Security.Cryptography;
using System.Text;
using BStorm.Tools.Encryptions.Cryptography.Asymmetric;
using BStorm.Tools.Encryptions.Cryptography.Symmetric;
using Isopoh.Cryptography.Argon2;

namespace CryptographySolution.Services;

public class CryptographyService
{
    IConfiguration _configuration;
    private string _key { get; set; }

    public CryptographyService(IConfiguration configuration)
    {
        _configuration = configuration;
        // get the aes key from appsettings
        // IV will be computed from the key
        this._key = _configuration["AesKey"];
    }

    public string EncryptAes(string content)
    {
        byte[] salt = SaltGenerator();
        byte[] key = Encoding.UTF8.GetBytes(this._key);

        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, salt, 10000);
        byte[] finalKey = pdb.GetBytes(32);
        byte[] iv = pdb.GetBytes(16);
        AesEncryption aes = new AesEncryption(finalKey, iv);
        byte[] cipherByte = aes.Encrypt(content);
        byte[] finalCipher = new byte[salt.Length + cipherByte.Length];
        Buffer.BlockCopy(salt, 0, finalCipher, 0, salt.Length);
        Buffer.BlockCopy(cipherByte, 0, finalCipher, salt.Length, cipherByte.Length);
        return Convert.ToBase64String(finalCipher);
    }

    public string DecryptAes(string cipher)
    {
        byte[] salt = new byte[32];
        byte[] key = Encoding.UTF8.GetBytes(this._key);
        byte[] data = Convert.FromBase64String(cipher);
        byte[] cipherBuffer = new byte[data.Length - 32];
        Buffer.BlockCopy(data, 0, salt, 0, 32);
        Buffer.BlockCopy(data, salt.Length, cipherBuffer, 0, cipherBuffer.Length);
        cipher = Convert.ToBase64String(cipherBuffer);

        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, salt, 10000);
        byte[] finalKey = pdb.GetBytes(32);
        byte[] iv = pdb.GetBytes(16);
        AesEncryption aes = new AesEncryption(finalKey, iv);

        return aes.Decrypt(cipher);
    }

    // Paramètres Argon2id minimum recommandés par l'OWASP (19 MiB, 2 itérations, 1 thread).
    private const int Argon2MemoryCost = 19456;
    private const int Argon2TimeCost = 2;
    private const int Argon2Lanes = 1;
    // Préfixe PHC d'un hash produit avec les paramètres actuels : tout autre hash doit être recalculé.
    private const string CurrentHashPrefix = "$argon2id$v=19$m=19456,t=2,p=1$";

    public string HashPassword(string password)
    {
        // Le résultat est au format PHC ($argon2id$v=19$m=...,t=...,p=...$sel$hash) : sel et paramètres y sont inclus.
        Argon2Config config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            MemoryCost = Argon2MemoryCost,
            TimeCost = Argon2TimeCost,
            Lanes = Argon2Lanes,
            Threads = Argon2Lanes,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = SaltGenerator(),
            HashLength = 32
        };

        return Argon2.Hash(config);
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Anciens comptes : hash BCrypt ($2a$, $2b$, ...) à vérifier avant leur migration vers Argon2id.
        if (hash.StartsWith("$2"))
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        return Argon2.Verify(hash, password);
    }

    public bool NeedsRehash(string hash)
    {
        return !hash.StartsWith(CurrentHashPrefix);
    }

    public string EncryptRsa(string message, string key)
    {
        RsaEncryption rsa = new RsaEncryption(key);
        return Convert.ToBase64String(rsa.Encrypt(message));
    }

    public string DecryptRsa(string message, string key)
    {
        RsaEncryption rsa = new RsaEncryption(key);
        return rsa.DecryptAsString(Convert.FromBase64String(message));
    }

    private byte[] SaltGenerator()
    {
        byte[] randomBytes = new byte[32];

        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }

        return randomBytes;
    }
}