using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class Bolum2_StoreOrder_DispatchPickList_DispatchPallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "DispatchOrders",
                newName: "StoreName");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DispatchOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DispatchOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "DispatchOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StoreOrderId",
                table: "DispatchOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DispatchBoxes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "DispatchBoxes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DispatchOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "int", nullable: false),
                    PickedQuantity = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchOrderItems_DispatchOrders_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchPallets_DispatchOrders_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPalletBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchPalletId = table.Column<int>(type: "int", nullable: false),
                    DispatchBoxId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPalletBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchPalletBoxes_DispatchBoxes_DispatchBoxId",
                        column: x => x.DispatchBoxId,
                        principalTable: "DispatchBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPalletBoxes_DispatchPallets_DispatchPalletId",
                        column: x => x.DispatchPalletId,
                        principalTable: "DispatchPallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreOrderItems_StoreOrders_StoreOrderId",
                        column: x => x.StoreOrderId,
                        principalTable: "StoreOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrders_StoreOrderId",
                table: "DispatchOrders",
                column: "StoreOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrderItems_DispatchOrderId",
                table: "DispatchOrderItems",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrderItems_ProductId",
                table: "DispatchOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPalletBoxes_DispatchBoxId",
                table: "DispatchPalletBoxes",
                column: "DispatchBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPalletBoxes_DispatchPalletId",
                table: "DispatchPalletBoxes",
                column: "DispatchPalletId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPallets_CompanyId_Barcode",
                table: "DispatchPallets",
                columns: new[] { "CompanyId", "Barcode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPallets_DispatchOrderId",
                table: "DispatchPallets",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrderItems_ProductId",
                table: "StoreOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrderItems_StoreOrderId",
                table: "StoreOrderItems",
                column: "StoreOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrders_CompanyId_OrderCode",
                table: "StoreOrders",
                columns: new[] { "CompanyId", "OrderCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Eski model altında oluşturulmuş test verisi yeni StoreOrder ilişkisiyle uyumsuz; temizlenir
            migrationBuilder.Sql("DELETE FROM DispatchBoxItems");
            migrationBuilder.Sql("DELETE FROM DispatchBoxes");
            migrationBuilder.Sql("DELETE FROM DispatchOrders");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrders_StoreOrders_StoreOrderId",
                table: "DispatchOrders",
                column: "StoreOrderId",
                principalTable: "StoreOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrders_StoreOrders_StoreOrderId",
                table: "DispatchOrders");

            migrationBuilder.DropTable(
                name: "DispatchOrderItems");

            migrationBuilder.DropTable(
                name: "DispatchPalletBoxes");

            migrationBuilder.DropTable(
                name: "StoreOrderItems");

            migrationBuilder.DropTable(
                name: "DispatchPallets");

            migrationBuilder.DropTable(
                name: "StoreOrders");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrders_StoreOrderId",
                table: "DispatchOrders");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "DispatchOrders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DispatchOrders");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "DispatchOrders");

            migrationBuilder.DropColumn(
                name: "StoreOrderId",
                table: "DispatchOrders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DispatchBoxes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DispatchBoxes");

            migrationBuilder.RenameColumn(
                name: "StoreName",
                table: "DispatchOrders",
                newName: "Destination");
        }
    }
}
