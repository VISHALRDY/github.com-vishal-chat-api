using Microsoft.EntityFrameworkCore;
using ChatAppApi.Models;

namespace ChatAppApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Message> Messages { get; set; }
}