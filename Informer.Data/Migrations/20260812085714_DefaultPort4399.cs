using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Informer.Data.Migrations
{
    public partial class DefaultPort4399 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Language", "ListenPort" },
                values: new object[] { "", 4399 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Language", "ListenPort" },
                values: new object[] { "ru", 5005 });
        }
    }
}
