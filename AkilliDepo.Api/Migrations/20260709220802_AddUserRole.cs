using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Staff");

            // Bu değişiklik öncesi tüm kullanıcılar zaten sınırsız/eşit yetkiye sahipti (RBAC hiç
            // yoktu); mevcut hesapların erişimini geriye dönük kısıtlamamak için hepsi Admin olarak
            // backfill edilir. Yeni oluşturulacak kullanıcılar için varsayılan Staff'tır.
            migrationBuilder.Sql("UPDATE Users SET Role = 'Admin' WHERE Role = 'Staff'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
