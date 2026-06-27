namespace pokemonTrainer.DTOs.DreamTeam;

public class DreamTeamResponse
{
    public int MaxSize { get; set; } = 5;

    public int CurrentSize { get; set; }

    public List<DreamTeamPokemonResponse> Items { get; set; } = new();
}