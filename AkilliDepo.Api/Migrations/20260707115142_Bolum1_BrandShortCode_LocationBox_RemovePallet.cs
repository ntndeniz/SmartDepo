using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class Bolum1_BrandShortCode_LocationBox_RemovePallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PalletBoxes");

            migrationBuilder.DropTable(
                name: "PalletLocationHistories");

            migrationBuilder.DropTable(
                name: "Pallets");

            migrationBuilder.AddColumn<int>(
                name: "CurrentBoxId",
                table: "Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "Brands",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CurrentBoxId",
                table: "Locations",
                column: "CurrentBoxId");

            // Var olan markalar için benzersiz geçici ShortCode ata (yeni unique index çakışmasın diye)
            migrationBuilder.Sql(
                "UPDATE Brands SET ShortCode = UPPER(LEFT(Name, 3)) + CAST(Id AS nvarchar(20)) WHERE ShortCode = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_CompanyId_ShortCode",
                table: "Brands",
                columns: new[] { "CompanyId", "ShortCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Boxes_CurrentBoxId",
                table: "Locations",
                column: "CurrentBoxId",
                principalTable: "Boxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Boxes_CurrentBoxId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_CurrentBoxId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Brands_CompanyId_ShortCode",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CurrentBoxId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "Brands");

            migrationBuilder.CreateTable(
                name: "Pallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PalletBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoxId = table.Column<int>(type: "int", nullable: false),
                    PalletId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PalletBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PalletBoxes_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PalletBoxes_Pallets_PalletId",
                        column: x => x.PalletId,
                        principalTable: "Pallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PalletLocationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    PalletId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PlacedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PalletLocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PalletLocationHistories_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PalletLocationHistories_Pallets_PalletId",
                        column: x => x.PalletId,
                        principalTable: "Pallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PalletBoxes_BoxId",
                table: "PalletBoxes",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_PalletBoxes_PalletId",
                table: "PalletBoxes",
                column: "PalletId");

            migrationBuilder.CreateIndex(
                name: "IX_PalletLocationHistories_LocationId",
                table: "PalletLocationHistories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PalletLocationHistories_PalletId",
                table: "PalletLocationHistories",
                column: "PalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_CompanyId_Barcode",
                table: "Pallets",
                columns: new[] { "CompanyId", "Barcode" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
