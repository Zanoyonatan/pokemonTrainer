export interface PokemonListItem {
  id?: number;
  pokeApiId: number;
  name: string;
  imageUrl?: string | null;
  types: string[];
  hp?: number;
  attack?: number;
  defense?: number;
  speed?: number;
  height?: number;
  weight?: number;
}

export interface PokemonDetails extends PokemonListItem {
  abilities?: string[];
  description?: string | null;
}
