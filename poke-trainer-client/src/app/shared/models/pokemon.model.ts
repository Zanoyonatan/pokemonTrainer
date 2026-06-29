export interface PokemonStat {
  name: string;
  baseStat: number;
  effort?: number;
}

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

  baseExperience?: number;
  isLegendary?: boolean;
  createdAt?: string;

  stats?: PokemonStat[];
}