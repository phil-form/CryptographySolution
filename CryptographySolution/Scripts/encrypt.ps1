# Usage : .\encrypt.ps1 -In AesKey.txt,dbString.txt,HttpsCertificate.pem,HttpsKey.pem -Out secrets.bin [-Scope CurrentUser]
# Chaque fichier devient une entrée de configuration nommée d'après son nom sans extension (ex. AesKey.txt -> "AesKey").
param(
    [Parameter(Mandatory)][string[]]$In,
    [Parameter(Mandatory)][string]$Out,
    [ValidateSet('LocalMachine', 'CurrentUser')][string]$Scope = 'LocalMachine'
)

Add-Type -AssemblyName System.Security

# Concatène les fichiers, chacun précédé du marqueur et de son nom, pour pouvoir les re-séparer au déchiffrement
$sep   = '@#@SECRET_START@#@'
$plain = ($In | ForEach-Object { "$sep $([IO.Path]::GetFileNameWithoutExtension($_))`n" + (Get-Content -Raw -LiteralPath $_).Trim() }) -join "`n"
$bytes = [Text.Encoding]::ASCII.GetBytes($plain + "`n")

# Chiffrement DPAPI : déchiffrable uniquement sur cette machine (ou par ce compte si -Scope CurrentUser)
$enc = [Security.Cryptography.ProtectedData]::Protect($bytes, $null, $Scope)

# Chemin absolu nécessaire : .NET résout les chemins relatifs depuis le répertoire du process, pas depuis le dossier courant PowerShell
$outPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out)
[IO.File]::WriteAllBytes($outPath, $enc)

# Efface le clair en mémoire
[Array]::Clear($bytes, 0, $bytes.Length)
