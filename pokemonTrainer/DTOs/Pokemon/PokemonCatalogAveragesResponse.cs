namespace pokemonTrainer.DTOs.Pokemon;

public sealed class PokemonCatalogAveragesResponse
{
    public double Hp { get; set; }
    public double Attack { get; set; }
    public double Defense { get; set; }
    public double SpecialAttack { get; set; }
    public double SpecialDefense { get; set; }
    public double Speed { get; set; }
    public double TotalStats { get; set; }
}