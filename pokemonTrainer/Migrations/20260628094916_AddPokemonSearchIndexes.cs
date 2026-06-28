using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pokemonTrainer.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Attack",
                table: "Pokemons",
                column: "Attack");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_BaseExperience",
                table: "Pokemons",
                column: "BaseExperience");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Defense",
                table: "Pokemons",
                column: "Defense");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Height",
                table: "Pokemons",
                column: "Height");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Hp",
                table: "Pokemons",
                column: "Hp");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_SpecialAttack",
                table: "Pokemons",
                column: "SpecialAttack");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_SpecialDefense",
                table: "Pokemons",
                column: "SpecialDefense");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Speed",
                table: "Pokemons",
                column: "Speed");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Weight",
                table: "Pokemons",
                column: "Weight");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Attack",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_BaseExperience",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Defense",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Height",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Hp",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_SpecialAttack",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_SpecialDefense",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Speed",
                table: "Pokemons");

            migrationBuilder.DropIndex(
                name: "IX_Pokemons_Weight",
                table: "Pokemons");
        }
    }
}
