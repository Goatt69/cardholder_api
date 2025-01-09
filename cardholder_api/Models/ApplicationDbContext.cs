using cardholder_api.Migrations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace cardholder_api.Models;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<PostModel> Posts { get; set; }
    public DbSet<CardHolder> CardHolders { get; set; }
    public DbSet<PokemonPost> PokemonPosts { get; set; }
    public DbSet<TradeOffer> TradeOffers { get; set; }
    public DbSet<OfferedCard> OfferCards { get; set; }
    public DbSet<NewsPost> NewsPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");
        builder.Entity<PokemonPost>().ToTable("PokemonPosts", "public");
        builder.Entity<CardHolder>().ToTable("CardHolders", "public");
        builder.Entity<pokemon_card>().ToTable("pokemon_cards", "public");
        builder.Entity<TradeOffer>().ToTable("TradeOffers", "public");
        builder.Entity<OfferedCard>().ToTable("OfferCards", "public");
        builder.Entity<NewsPost>().ToTable("NewsPosts", "public");
        builder.Entity<User>().Property(u => u.Initials).HasMaxLength(5);
    }
}