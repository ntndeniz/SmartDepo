using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorridorCount = table.Column<int>(type: "int", nullable: false),
                    ZonesPerCorridor = table.Column<int>(type: "int", nullable: false),
                    ShelvesPerZone = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_CompanyId",
                table: "CompanySettings",
                column: "CompanyId",
                unique: true,
                filter: "[IsDeleted] = 0");

            // Zaten konum üretmiş şirketler için mevcut Location verisinden depo boyutunu geriye
            // dönük çıkarsa, aksi halde "Toplu Üretim" ayarlanmamış hatası verir.
            migrationBuilder.Sql(@"
                INSERT INTO CompanySettings (CorridorCount, ZonesPerCorridor, ShelvesPerZone, UpdatedAt, CompanyId, IsDeleted)
                SELECT MAX(CorridorNo), MAX(ZoneNo), MAX(ShelfNo), GETUTCDATE(), CompanyId, 0
                FROM Locations
                WHERE IsDeleted = 0
                GROUP BY CompanyId
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanySettings");
        }
    }
}
