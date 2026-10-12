# CryptographySolution

API ASP.NET Core 8 de démonstration en cryptographie, recréée à partir de
[DemoCryptographyApi](https://gitlab.com/philippe.stons/DemoCryptographyApi). Elle utilise
[BStorm.Tools.Encryptions](https://github.com/TMCognitic/BStorm.Tools.Encryptions) 1.0.3.

| Donnée | Protection |
|---|---|
| Mot de passe | Hash **Argon2id** (`m=19456,t=2,p=1`). Les anciens hash BCrypt sont migrés à la connexion |
| `secretMessage` de l'utilisateur | **AES-256**, clé dérivée de `AesKey` par PBKDF2 avec un sel aléatoire |
| Messages entre utilisateurs | **AES-256**, même clé `AesKey` que `secretMessage` (PBKDF2, sel aléatoire par message) |
| Secrets de l'application (`AesKey`, `dbString`, certificat HTTPS) | Fichier `Secrets/secrets.bin` chiffré pour cette machine (`systemd-creds` sous Linux, DPAPI sous Windows) |

## Prérequis

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- Docker (pour PostgreSQL)
- Linux : systemd ≥ 256 (pour `systemd-creds --user`) et `openssl`
- L'outil Entity Framework : `dotnet tool install -g dotnet-ef --version 8.0.13`

## 1. Créer les secrets

L'application lit ses secrets dans `CryptographySolution/Secrets/secrets.bin`. Ce fichier ne peut être
déchiffré que sur la machine (et, en scope `user`, par le compte) qui l'a créé. Il est ignoré par git :
chaque poste doit donc générer le sien.

Chaque fichier passé au script devient une entrée de configuration qui porte son nom sans
extension. Les 4 entrées attendues sont les suivantes :

| Fichier | Contenu |
|---|---|
| `AesKey.txt` | Clé AES (ex. `1234567890123456`) |
| `dbString.txt` | `Host=localhost;Port=5435;Username=app;Password=1234;Database=app` |
| `HttpsCertificate.pem` | Certificat HTTPS (PEM) |
| `HttpsKey.pem` | Clé privée du certificat (PEM, non chiffrée) |

### Linux

```bash
cd CryptographySolution
tmp=$(mktemp -d) && chmod 700 "$tmp"

printf '1234567890123456' > "$tmp/AesKey.txt"
printf 'Host=localhost;Port=5435;Username=app;Password=1234;Database=app' > "$tmp/dbString.txt"

# Certificat auto-signé pour localhost, valable 1 an
openssl req -x509 -newkey rsa:3072 -sha256 -days 365 -nodes -subj "/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1" \
  -addext "extendedKeyUsage=serverAuth" \
  -keyout "$tmp/HttpsKey.pem" -out "$tmp/HttpsCertificate.pem"

mkdir -p Secrets
Scripts/encrypt.sh -o Secrets/secrets.bin \
  "$tmp/AesKey.txt" "$tmp/dbString.txt" "$tmp/HttpsCertificate.pem" "$tmp/HttpsKey.pem"

# Supprime les fichiers en clair
shred -u "$tmp"/* && rmdir "$tmp"
```

`encrypt.sh` chiffre par défaut avec `-s user`, comme `CurrentUser` : seul ton compte peut relire le
fichier. `-s machine` correspond à `LocalMachine`, mais il faut être root pour chiffrer **et** pour
déchiffrer. Dans ce cas, mets `"Scope": "machine"` dans `appsettings.json` et lance l'application en root
ou comme service systemd.

Pour vérifier le contenu du fichier :

```bash
systemd-creds --user decrypt --name=secrets.bin Secrets/secrets.bin -
```

### Windows

1. Dans `Configuration/EncryptedSecrets.cs`, décommente le bloc `OperatingSystem.IsWindows()` et ajoute
   le package `System.Security.Cryptography.ProtectedData`.
2. Dans `Program.cs`, décommente la ligne de ré-import PKCS#12 du certificat.
3. Dans `appsettings.json`, mets `"Scope": "user"` pour `CurrentUser`, ou toute autre valeur pour `LocalMachine`.
4. Génère le certificat avec `dotnet dev-certs https --export-path HttpsCertificate.pem --format Pem --no-password`.
   Cette commande crée aussi `HttpsCertificate.key` : renomme-le en `HttpsKey.pem`.
5. Lance le script de chiffrement :

```powershell
.\Scripts\encrypt.ps1 -In AesKey.txt,dbString.txt,HttpsCertificate.pem,HttpsKey.pem -Out Secrets\secrets.bin -Scope CurrentUser
```

## 2. Démarrer PostgreSQL

```bash
cd CryptographySolution
docker compose up -d
```

La base écoute sur le port **5435**. Si ce port est déjà utilisé, modifie `docker-compose.yml` et le
`dbString` du `.bin`.

## 3. Appliquer les migrations

```bash
dotnet ef database update
```

## 4. Lancer l'API

```bash
dotnet run
```

Le profil `https` est utilisé par défaut :

- API : https://localhost:7231 (http://localhost:5259 redirige vers HTTPS)
- Swagger : https://localhost:7231/swagger

Le certificat est auto-signé, le navigateur affichera donc un avertissement. Pour l'éviter, ajoute-le
aux certificats de confiance de ta machine ou utilise un certificat signé par une autorité reconnue.

## Endpoints

| Méthode | Route | Description |
|---|---|---|
| `POST` | `/api/user` | Crée un utilisateur `{ username, password, secretMessage }` |
| `POST` | `/api/user/login` | `{ username, password }` renvoie l'utilisateur avec son secret déchiffré (204 si identifiants invalides) |
| `GET` | `/api/user` | Liste les utilisateurs (secrets chiffrés) |
| `GET` | `/api/user/{id}` | Un utilisateur (404 s'il n'existe pas) |
| `PUT` | `/api/user/{id}/edit` | Modifie `{ username, password, secretMessage }` (404 s'il n'existe pas) |
| `POST` | `/api/message` | Envoie `{ message, senderId, receiverId }`, chiffré avec la clé publique du destinataire |
| `GET` | `/api/message` | Tous les messages (chiffrés) |
| `GET` | `/api/message/{userId}` | Messages reçus par l'utilisateur, déchiffrés |

Des requêtes prêtes à l'emploi se trouvent dans `CryptographySolution/CryptographySolution.http`.

Un message RSA est limité à 235 caractères : avec une clé de 4096 bits et l'encodage UTF-16,
on peut chiffrer au maximum 470 octets.

## Limites de la démo

Il s'agit d'un projet pédagogique. Comme le précisent les commentaires du projet d'origine :

- Les clés privées RSA sont stockées en clair en base. Elles devraient être chiffrées, par exemple avec le mot de passe de l'utilisateur.
- Le chiffrement RSA des messages devrait se faire côté client.
- L'API n'a pas d'authentification : tous les endpoints sont accessibles sans être connecté.
