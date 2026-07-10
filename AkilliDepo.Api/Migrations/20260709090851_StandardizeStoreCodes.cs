using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeStoreCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski StoreCode'lar farklı uzunluklarda serbest metindi (ör. "dmall", "MGZ001", "MGZTEST").
            // Hepsini Brand.ShortCode ile aynı desene (isim baz alınarak üretilen, 3 karakter) çevirir
            // ve StoreOrder/DispatchOrder/DispatchPallet'teki denormalize StoreId kopyalarını günceller.
            migrationBuilder.Sql(@"
                IF OBJECT_ID('tempdb..#StoreCodeMap') IS NOT NULL DROP TABLE #StoreCodeMap;

                SELECT
                    s.Id,
                    s.CompanyId,
                    s.StoreCode AS OldCode,
                    LEFT(UPPER(REPLACE(REPLACE(REPLACE(s.Name, ' ', ''), '-', ''), '.', '')) + 'XXX', 3) AS NewCode
                INTO #StoreCodeMap
                FROM Stores s
                WHERE s.IsDeleted = 0;

                -- Aynı şirket içinde aynı 3 karakterlik koda düşen isimler için son karakteri
                -- A/B/C... ile ayırarak eşsizliği koru.
                ;WITH Ranked AS (
                    SELECT *, ROW_NUMBER() OVER (PARTITION BY CompanyId, NewCode ORDER BY Id) AS rn
                    FROM #StoreCodeMap
                )
                UPDATE Ranked
                SET NewCode = LEFT(NewCode, 2) + CHAR(65 + ((rn - 1) % 26))
                WHERE rn > 1;

                UPDATE s
                SET s.StoreCode = m.NewCode
                FROM Stores s
                JOIN #StoreCodeMap m ON s.Id = m.Id;

                UPDATE so
                SET so.StoreId = m.NewCode
                FROM StoreOrders so
                JOIN #StoreCodeMap m ON so.CompanyId = m.CompanyId AND so.StoreId = m.OldCode;

                UPDATE do_
                SET do_.StoreId = m.NewCode
                FROM DispatchOrders do_
                JOIN #StoreCodeMap m ON do_.CompanyId = m.CompanyId AND do_.StoreId = m.OldCode;

                UPDATE dp
                SET dp.StoreId = m.NewCode
                FROM DispatchPallets dp
                JOIN #StoreCodeMap m ON dp.CompanyId = m.CompanyId AND dp.StoreId = m.OldCode;

                DROP TABLE #StoreCodeMap;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
