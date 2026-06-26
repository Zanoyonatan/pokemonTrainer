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
    }
}