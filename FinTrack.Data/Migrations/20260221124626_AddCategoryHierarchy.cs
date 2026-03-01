using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21,
                column: "ParentCategoryId",
                value: null);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "ParentCategoryId", "Type" },
                values: new object[,]
                {
                    { 22, "Elektrik", 5, 1 },
                    { 23, "Su", 5, 1 },
                    { 24, "Doğalgaz", 5, 1 },
                    { 25, "İnternet & TV", 5, 1 },
                    { 26, "Telefon", 5, 1 },
                    { 27, "Aidat", 5, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categories");
        }
    }
}
