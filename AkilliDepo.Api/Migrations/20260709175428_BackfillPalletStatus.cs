using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillPalletStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bu migration öncesi oluşturulmuş paletler eski akışta oluşturulduğu anda sevkiyata
            // hazır kabul ediliyordu; yeni yaşam döngüsüyle uyumlu olsun diye "Shipped" olarak
            // işaretleniyor (zaten sevk edilmiş kabul edilir).
            migrationBuilder.Sql("UPDATE DispatchPallets SET Status = 'Shipped' WHERE Status = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
