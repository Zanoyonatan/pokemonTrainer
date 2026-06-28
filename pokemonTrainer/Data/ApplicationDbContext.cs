using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using pokemonTrainer.Models;

namespace pokemonTrainer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pokemon> Pokemons { get; set; }

    public DbSet<PokemonType> PokemonTypes { get; set; }

    public DbSet<PokemonPokemonType> PokemonPokemonTypes { get; set; }

    public DbSet<DreamTeamPokemon> DreamTeamPokemons { get; set; }
    public DbSet<PokemonCatalogState> PokemonCatalogStates { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Pokemon>()
            .HasIndex(p => p.PokeApiId)
            .IsUnique();

        builder.Entity<Pokemon>()
            .HasIndex(p => p.Name);

        builder.Entity<PokemonType>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<PokemonPokemonType>()
            .HasKey(pt => new { pt.PokemonId, pt.PokemonTypeId });

        builder.Entity<PokemonPokemonType>()
            .HasOne(pt => pt.Pokemon)
            .WithMany(p => p.PokemonTypes)
            .HasForeignKey(pt => pt.PokemonId);

        builder.Entity<PokemonPokemonType>()
            .HasOne(pt => pt.PokemonType)
            .WithMany(t => t.PokemonTypes)
            .HasForeignKey(pt => pt.PokemonTypeId);

        builder.Entity<DreamTeamPokemon>()
    .HasIndex(d => new { d.UserId, d.PokemonId })
    .IsUnique();

        builder.Entity<DreamTeamPokemon>()
            .HasIndex(d => new { d.UserId, d.Slot })
            .IsUnique();

        builder.Entity<DreamTeamPokemon>()
            .Property(d => d.Nickname)
            .HasMaxLength(50);

        builder.Entity<DreamTeamPokemon>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DreamTeamPokemon>()
            .HasOne(d => d.Pokemon)
            .WithMany()
            .HasForeignKey(d => d.PokemonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PokemonCatalogState>()
        .HasKey(s => s.Id);
    }
}