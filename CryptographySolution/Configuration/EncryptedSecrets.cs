using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;

namespace CryptographySolution.Configuration;

// Lit le fichier .bin produit par Scripts/encrypt.sh (Linux) ou encrypt.ps1 (Windows)
// et ajoute chaque secret à la configuration (ex. section "AesKey" -> _configuration["AesKey"]).
public static class EncryptedSecrets
{
    private const string Separator = "@#@SECRET_START@#@";

    public static IConfigurationBuilder AddEncryptedSecrets(this IConfigurationBuilder builder, string path, string scope)
    {
        byte[] plain = Decrypt(path, scope);

        try
        {
            return builder.AddInMemoryCollection(Parse(Encoding.ASCII.GetString(plain)));
        }
        finally
        {
            // Efface le clair en mémoire
            Array.Clear(plain, 0, plain.Length);
        }
    }

    private static byte[] Decrypt(string path, string scope)
    {
        // Windows (DPAPI, fichier produit par encrypt.ps1) — nécessite le package System.Security.Cryptography.ProtectedData
        // et "using System.Security.Cryptography;". Pour que Parse() retrouve le nom des secrets, encrypt.ps1 doit écrire
        // le nom après le marqueur : "$sep $([IO.Path]::GetFileNameWithoutExtension($_))`n" au lieu de "$sep`n".
        if (OperatingSystem.IsWindows())
        {
            DataProtectionScope dpScope = scope == "user" ? DataProtectionScope.CurrentUser : DataProtectionScope.LocalMachine;
            return ProtectedData.Unprotect(File.ReadAllBytes(path), null, dpScope);
        }

        // Linux : systemd-creds vérifie que le fichier a été chiffré sur cette machine (et pour ce compte en scope "user").
        ProcessStartInfo startInfo = new ProcessStartInfo("systemd-creds")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (scope == "user")
        {
            startInfo.ArgumentList.Add("--user");
        }
        startInfo.ArgumentList.Add("decrypt");
        startInfo.ArgumentList.Add($"--name={Path.GetFileName(path)}");
        startInfo.ArgumentList.Add(path);
        startInfo.ArgumentList.Add("-");

        using Process process = Process.Start(startInfo)!;
        using MemoryStream output = new MemoryStream();
        Task<string> error = process.StandardError.ReadToEndAsync();
        process.StandardOutput.BaseStream.CopyTo(output);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Impossible de déchiffrer {path} : {error.Result.Trim()}");
        }

        return output.ToArray();
    }

    private static Dictionary<string, string?> Parse(string content)
    {
        Dictionary<string, string?> secrets = new Dictionary<string, string?>();

        // Chaque section : "<marqueur> <nom>\n<valeur>"
        foreach (string section in content.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = section.Split('\n', 2);
            string name = parts[0].Trim();

            if (name.Length > 0)
            {
                secrets[name] = parts.Length > 1 ? parts[1].Trim() : "";
            }
        }

        return secrets;
    }
}
