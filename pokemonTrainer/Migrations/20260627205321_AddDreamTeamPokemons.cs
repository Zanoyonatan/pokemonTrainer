using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pokemonTrainer.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamTeamPokemons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DreamTeamPokemons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PokemonId = table.Column<int>(type: "int", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamTeamPokemons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DreamTeamPokemons_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DreamTeamPokemons_Pokemons_PokemonId",
                        column: x => x.PokemonId,
                        principalTable: "Pokemons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DreamTeamPokemons_PokemonId",
                table: "DreamTeamPokemons",
                column: "PokemonId");

            migrationBuilder.CreateIndex(
                name: "IX_DreamTeamPokemons_UserId_PokemonId",
                table: "DreamTeamPokemons",
                columns: new[] { "UserId", "PokemonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DreamTeamPokemons_UserId_Slot",
                table: "DreamTeamPokemons",
                columns: new[] { "UserId", "Slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DreamTeamPokemons");
        }
    }
}
