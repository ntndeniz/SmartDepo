using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanupDuplicateTestDispatchOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aynı sipariş barkodu birden fazla kez okutulduğunda (çift tarama/tıklama) her seferinde
            // yeni bir DispatchOrder oluşturuluyordu (bkz. DispatchManager.CreateFromStoreOrderAsync
            // düzeltmesi) — bu da StoreOrderManager.GetPagedAsync'teki ToDictionary'de "aynı anahtar
            // iki kez eklenemez" istisnasıyla 500 hatasına yol açıyordu. Bu düzeltme öncesi oluşmuş
            // yinelenen kayıtları geriye dönük temizler: her StoreOrderId için en çok ilerlemiş olanı
            // (en fazla kolisi olan, sonra en yeni) korur, diğerlerini soft-delete eder.
            migrationBuilder.Sql(@"
                ;WITH ranked AS (
                    SELECT d.Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY d.StoreOrderId
                            ORDER BY
                                (SELECT COUNT(*) FROM DispatchBoxes b WHERE b.DispatchOrderId = d.Id AND b.IsDeleted = 0) DESC,
                                d.Id DESC
                        ) AS rn
                    FROM DispatchOrders d
                    WHERE d.IsDeleted = 0
                )
                UPDATE DispatchOrders SET IsDeleted = 1
                WHERE Id IN (SELECT Id FROM ranked WHERE rn > 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
