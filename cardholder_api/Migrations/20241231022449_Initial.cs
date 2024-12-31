using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace cardholder_api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");
            
            migrationBuilder.CreateTable(
                name: "PokemonPosts",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardId = table.Column<string>(type: "text", nullable: false),
                    PosterId = table.Column<string>(type: "text", nullable: false),
                    BuyerId = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonPosts_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PokemonPosts_AspNetUsers_PosterId",
                        column: x => x.PosterId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonPosts_pokemon_card_CardId",
                        column: x => x.CardId,
                        principalSchema: "public",
                        principalTable: "pokemon_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonPosts_BuyerId",
                schema: "public",
                table: "PokemonPosts",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonPosts_CardId",
                schema: "public",
                table: "PokemonPosts",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonPosts_PosterId",
                schema: "public",
                table: "PokemonPosts",
                column: "PosterId");
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
