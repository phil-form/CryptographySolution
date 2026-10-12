using Microsoft.EntityFrameworkCore;
using CryptographySolution.Models;

namespace CryptographySolution.Services;

public class DemoDbContext: DbContext
{
    private readonly IConfiguration _configuration;
    public DemoDbContext(DbContextOptions<DemoDbContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration["dbString"]);
    }
}