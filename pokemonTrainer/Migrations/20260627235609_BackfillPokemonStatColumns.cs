using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pokemonTrainer.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPokemonStatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
UPDATE p
SET
    Hp = COALESCE(s.Hp, 0),
    Attack = COALESCE(s.Attack, 0),
    Defense = COALESCE(s.Defense, 0),
    SpecialAttack = COALESCE(s.SpecialAttack, 0),
    SpecialDefense = COALESCE(s.SpecialDefense, 0),
    Speed = COALESCE(s.Speed, 0)
FROM Pokemons p
OUTER APPLY
(
    SELECT
        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'hp'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS Hp,

        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'attack'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS Attack,

        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'defense'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS Defense,

        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'special-attack'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS SpecialAttack,

        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'special-defense'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS SpecialDefense,

        MAX(CASE WHEN JSON_VALUE([value], '$.Name') = 'speed'
            THEN TRY_CAST(JSON_VALUE([value], '$.BaseStat') AS int) END) AS Speed
    FROM OPENJSON(p.StatsJson)
) s
WHERE p.StatsJson IS NOT NULL;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
UPDATE Pokemons
SET
    Hp = 0,
    Attack = 0,
    Defense = 0,
    SpecialAttack = 0,
    SpecialDefense = 0,
    Speed = 0;
""");
        }
    }
}
