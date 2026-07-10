using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixOrphanLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Box soft-delete bugı (bkz. BoxRepository) yüzünden bazı raflar, artık silinmiş kolilere
            // işaret eden "dolu" durumda kalmış olabilir — bu düzeltme onları boşaltır.
            migrationBuilder.Sql(@"
                UPDATE Locations
                SET IsOccupied = 0, CurrentBoxId = NULL
                WHERE CurrentBoxId IN (SELECT Id FROM Boxes WHERE IsDeleted = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
