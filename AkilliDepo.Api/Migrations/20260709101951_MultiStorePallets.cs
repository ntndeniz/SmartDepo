using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class MultiStorePallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchPallets_DispatchOrders_DispatchOrderId",
                table: "DispatchPallets");

            migrationBuilder.DropIndex(
                name: "IX_DispatchPallets_DispatchOrderId",
                table: "DispatchPallets");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "DispatchPallets");

            migrationBuilder.DropColumn(
                name: "DispatchOrderId",
                table: "DispatchPallets");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "DispatchPallets");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "DispatchPallets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DispatchPallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DispatchOrderId",
                table: "DispatchPallets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "DispatchPallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "DispatchPallets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPallets_DispatchOrderId",
                table: "DispatchPallets",
                column: "DispatchOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchPallets_DispatchOrders_DispatchOrderId",
                table: "DispatchPallets",
                column: "DispatchOrderId",
                principalTable: "DispatchOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
