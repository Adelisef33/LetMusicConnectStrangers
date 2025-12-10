using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LetMusicConnectStrangers.Models;

namespace LetMusicConnectStrangers.Data;

public class LetMusicConnectStrangersContext : IdentityDbContext<ApplicationUser>
{
    public LetMusicConnectStrangersContext(DbContextOptions<LetMusicConnectStrangersContext> options)
        : base(options)
    {
    }

    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
