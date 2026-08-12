using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Informer.Data.Migrations
{
    public partial class AddSeverityAndToastPolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Notifications",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowErrorToasts",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInfoToasts",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowWarningToasts",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ShowErrorToasts", "ShowInfoToasts", "ShowWarningToasts" },
                values: new object[] { true, true, true });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ShowErrorToasts",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ShowInfoToasts",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ShowWarningToasts",
                table: "AppSettings");
        }
    }
}
