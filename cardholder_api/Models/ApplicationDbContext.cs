using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }
        public DbSet<Product> Products { get; set; }
        public DbSet<PostModel> Posts { get; set; }
        public DbSet<CardHolder> CardHolders { get; set; }
        public DbSet<PokemonPost> PokemonPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<User>().Property(u => u.Initials).HasMaxLength(5);
            builder.HasDefaultSchema("identity");
        }

    }
}