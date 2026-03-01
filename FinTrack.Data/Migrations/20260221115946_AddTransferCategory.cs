using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Type" },
                values: new object[] { 21, "Kredi Kartı Ödemesi", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21);
        }
    }
}
