using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanupEmptyBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Boş koli olamaz" düzeltmesi öncesi, miktarı 0'a düşmüş (manuel Stok Düzeltme) koliler
            // soft-delete edilmeden listede kalmaya devam ediyordu. NOT: DispatchBoxItem.SourceBoxId
            // tarafından referans verilen (picking ile tüketilmiş, Status=Dispatched) koliler bilerek
            // HARİÇ tutulur — bunlar geçmiş sevkiyat kayıtlarının parçasıdır ve SourceBoxId non-nullable
            // FK olduğu için soft-delete edilirse EF'in Include'u (INNER JOIN) o geçmiş kayıtları
            // sessizce sorgu sonucundan düşürür (bkz. Box↔Product bug'ı ile aynı sınıf hata).
            migrationBuilder.Sql(@"
                UPDATE Locations
                SET IsOccupied = 0, CurrentBoxId = NULL
                WHERE CurrentBoxId IN (
                    SELECT Id FROM Boxes
                    WHERE Quantity = 0 AND IsDeleted = 0
                      AND Id NOT IN (SELECT SourceBoxId FROM DispatchBoxItems))");

            migrationBuilder.Sql(@"
                UPDATE Boxes SET IsDeleted = 1
                WHERE Quantity = 0 AND IsDeleted = 0
                  AND Id NOT IN (SELECT SourceBoxId FROM DispatchBoxItems)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
