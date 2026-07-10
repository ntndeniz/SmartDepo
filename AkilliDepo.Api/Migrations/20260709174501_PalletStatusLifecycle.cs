using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class PalletStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DispatchPallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DispatchPallets");
        }
    }
}
