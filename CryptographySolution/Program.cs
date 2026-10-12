using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using CryptographySolution.Configuration;
using CryptographySolution.Mappers;
using CryptographySolution.Services;

var builder = WebApplication.CreateBuilder(args);

// Secrets (AesKey, dbString, certificat HTTPS) chiffrés dans un .bin lisible uniquement sur cette machine (voir Scripts/encrypt.sh)
builder.Configuration.AddEncryptedSecrets(
    Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Secrets:Path"]!),
    builder.Configuration["Secrets:Scope"]!);

// HTTPS : certificat et clé privée (PEM) issus du .bin, utilisés par tous les endpoints https de Kestrel
X509Certificate2 httpsCertificate = X509Certificate2.CreateFromPem(
    builder.Configuration["HttpsCertificate"], builder.Configuration["HttpsKey"]);
// Windows : SslStream refuse les clés éphémères de CreateFromPem, il faut ré-importer le certificat via un PKCS#12 :
// httpsCertificate = new X509Certificate2(httpsCertificate.Export(X509ContentType.Pkcs12));
builder.WebHost.ConfigureKestrel(options =>
    options.ConfigureHttpsDefaults(https => https.ServerCertificate = httpsCertificate));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DemoDbContext>();
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<MessageMapper>();
builder.Services.AddScoped<CryptographyService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    DbSeeder.Seed(app.Services);
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
