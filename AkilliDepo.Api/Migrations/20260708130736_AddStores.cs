using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_CompanyId_Name",
                table: "Stores",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_CompanyId_StoreCode",
                table: "Stores",
                columns: new[] { "CompanyId", "StoreCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Mevcut StoreOrder kayıtlarındaki mağazaları geriye dönük Stores tablosuna aktar,
            // aksi halde eski mağazalar bir daha sipariş verdiğinde adı zaten var olduğu için
            // GetOrCreateAsync tarafından yanlışlıkla "yeni mağaza" sanılmaz.
            migrationBuilder.Sql(@"
                INSERT INTO Stores (StoreCode, Name, Address, CreatedAt, CompanyId, IsDeleted)
                SELECT MAX(StoreId), StoreName, MAX(Address), MIN(CreatedAt), CompanyId, 0
                FROM StoreOrders
                WHERE IsDeleted = 0
                GROUP BY CompanyId, StoreName
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stores");
        }
    }
}
