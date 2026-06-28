# STEP 06 — Backend Search Indexes

## Purpose

Add database indexes to support better performance for Pokémon search and Smart Search scenarios.

This step should only modify the database model configuration and create a migration for search-related indexes.

Do not change controllers, services, DTOs, authentication, AI logic, import logic, or unrelated files.

---

## Background

The backend already supports Smart Search queries such as:

```text
fast electric pokemon
strong fire pokemon
high hp water pokemon
top 10 small electric pokemon
```

These queries commonly filter or sort by Pokémon fields such as:

- Name
- PokeApiId
- Hp
- Attack
- Defense
- SpecialAttack
- SpecialDefense
- Speed
- Height
- Weight
- BaseExperience

The goal of this step is to add indexes for the columns that are frequently used in filtering and sorting.

---

## Files to Modify

Modify only:

```text
Data/ApplicationDbContext.cs
```

Then create a migration.

---

## Required Change

Open:

```text
Data/ApplicationDbContext.cs
```

Inside `OnModelCreating`, locate the existing Pokémon configuration.

There should already be indexes like:

```csharp
builder.Entity<Pokemon>()
    .HasIndex(p => p.PokeApiId)
    .IsUnique();

builder.Entity<Pokemon>()
    .HasIndex(p => p.Name);
```

Do not duplicate existing indexes.

Add the following indexes under the existing Pokémon indexes:

```csharp
builder.Entity<Pokemon>()
    .HasIndex(p => p.Hp);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Attack);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Defense);

builder.Entity<Pokemon>()
    .HasIndex(p => p.SpecialAttack);

builder.Entity<Pokemon>()
    .HasIndex(p => p.SpecialDefense);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Speed);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Height);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Weight);

builder.Entity<Pokemon>()
    .HasIndex(p => p.BaseExperience);
```

---

## Example Pokémon Configuration

The relevant section should look similar to this:

```csharp
builder.Entity<Pokemon>()
    .HasIndex(p => p.PokeApiId)
    .IsUnique();

builder.Entity<Pokemon>()
    .HasIndex(p => p.Name);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Hp);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Attack);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Defense);

builder.Entity<Pokemon>()
    .HasIndex(p => p.SpecialAttack);

builder.Entity<Pokemon>()
    .HasIndex(p => p.SpecialDefense);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Speed);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Height);

builder.Entity<Pokemon>()
    .HasIndex(p => p.Weight);

builder.Entity<Pokemon>()
    .HasIndex(p => p.BaseExperience);
```

Keep the rest of the existing model configuration unchanged.

---

## Migration

After updating `ApplicationDbContext.cs`, run:

```powershell
Add-Migration AddPokemonSearchIndexes
```

Then run:

```powershell
Update-Database
```

---

## SQL Validation

After the migration is applied, run this SQL query:

```sql
SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    c.name AS ColumnName
FROM sys.indexes i
JOIN sys.index_columns ic 
    ON i.object_id = ic.object_id 
   AND i.index_id = ic.index_id
JOIN sys.columns c 
    ON ic.object_id = c.object_id 
   AND ic.column_id = c.column_id
JOIN sys.tables t 
    ON i.object_id = t.object_id
WHERE t.name = 'Pokemons'
ORDER BY i.name;
```

Expected columns to appear in indexes:

```text
PokeApiId
Name
Hp
Attack
Defense
SpecialAttack
SpecialDefense
Speed
Height
Weight
BaseExperience
```

---

## API Validation

After applying the migration, verify that existing endpoints still work:

### Smart Search

```http
POST /api/pokemon-smart-search
Authorization: Bearer {token}
```

Body:

```json
{
  "query": "find me top 10 small electric pokemon",
  "page": 1,
  "pageSize": 5
}
```

Expected:

- Request succeeds.
- Results are returned from SQL Server.
- `items.length <= 5`.
- Criteria includes:
  - `type = electric`
  - `maxHeight = 10`
  - `requestedCount = 10`
  - `sortBy = totalStats`

### Pokémon list

```http
GET /api/pokemon?page=1&pageSize=10
Authorization: Bearer {token}
```

Expected:

- Request succeeds.
- Pokémon list returns normally.

---

## Notes

For the current dataset size, around 1,300 Pokémon, the performance benefit may not be dramatic.

However, these indexes demonstrate proper backend design because Smart Search frequently sorts and filters by these stat columns.

The goal is not to over-index every possible column, but to support the most common query patterns.

---

## Design Explanation

The Smart Search feature translates natural-language queries into structured SQL filters and sorting rules. Since common queries sort by fields such as Speed, Attack, Defense, HP, Height, Weight, and BaseExperience, indexes on these columns help SQL Server execute these searches more efficiently.

This also prepares the backend for future growth if more Pokémon metadata, custom collections, or larger datasets are added later.
