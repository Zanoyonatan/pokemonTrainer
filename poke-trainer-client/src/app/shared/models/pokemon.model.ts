export interface PokemonListItem { id: number; name: string; imageUrl?: string | null; types: string[]; hp?: number; attack?: number; defense?: number; speed?: number; }
export interface PokemonDetails extends PokemonListItem { height?: number; weight?: number; abilities?: string[]; description?: string | null; }
