using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // .ToUpper() -> .ToUpperInvariant() düzeltmesi yalnızca YENİ üretilen barkodları etkiledi;
            // Türkçe kültürde 'i'.ToUpper() -> 'İ' (U+0130) üretiyordu, bu karakter CODE128'de geçersiz
            // olduğu için barkod yazdırma bu kayıtlarda sessizce başarısız oluyordu. Burada tüm
            // barkod/kod taşıyan sütunlarda İ/ı harfleri ASCII I ile değiştirilerek normalize edilir.
            string[] tableColumns =
            {
                "Products.Barcode", "Boxes.Barcode", "DispatchBoxes.Barcode",
                "DispatchPallets.Barcode", "Stores.StoreCode", "StoreOrders.OrderCode", "Brands.ShortCode"
            };

            foreach (var tc in tableColumns)
            {
                var parts = tc.Split('.');
                var table = parts[0];
                var column = parts[1];
                migrationBuilder.Sql($@"
                    UPDATE [{table}]
                    SET [{column}] = REPLACE(REPLACE([{column}], N'İ', N'I'), N'ı', N'I')
                    WHERE [{column}] LIKE N'%İ%' OR [{column}] LIKE N'%ı%'");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
