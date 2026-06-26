using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pokemonTrainer.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pokemons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PokeApiId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    BaseExperience = table.Column<int>(type: "int", nullable: true),
                    StatsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsLegendary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pokemons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonPokemonTypes",
                columns: table => new
                {
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    PokemonTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonPokemonTypes", x => new { x.PokemonId, x.PokemonTypeId });
                    table.ForeignKey(
                        name: "FK_PokemonPokemonTypes_PokemonTypes_PokemonTypeId",
                        column: x => x.PokemonTypeId,
                        principalTable: "PokemonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonPokemonTypes_Pokemons_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonPokemonTypes_PokemonTypeId",
                table: "PokemonPokemonTypes",
                column: "PokemonTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_Name",
                table: "Pokemons",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Pokemons_PokeApiId",
                table: "Pokemons",
                column: "PokeApiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonTypes_Name",
                table: "PokemonTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokemonPokemonTypes");

            migrationBuilder.DropTable(
                name: "PokemonTypes");

            migrationBuilder.DropTable(
                name: "Pokemons");
        }
    }
}
