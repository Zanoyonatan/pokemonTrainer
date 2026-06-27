using System.Net.Http.Json;
using pokemonTrainer.DTOs.PokeApi;

namespace pokemonTrainer.Infrastructure;

public class PokeApiClient
{
    private readonly HttpClient _httpClient;

    public PokeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PokeApiListResponse?> GetPokemonListAsync(int limit, int offset = 0)
    {
        return await _httpClient.GetFromJsonAsync<PokeApiListResponse>(
            $"pokemon?limit={limit}&offset={offset}");
    }

    public async Task<PokeApiPokemonDetails?> GetPokemonDetailsAsync(string nameOrId)
    {
        return await _httpClient.GetFromJsonAsync<PokeApiPokemonDetails>(
            $"pokemon/{nameOrId}");
    }
}