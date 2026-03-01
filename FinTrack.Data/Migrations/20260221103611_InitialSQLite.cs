using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSQLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BankName = table.Column<string>(type: "TEXT", nullable: false),
                    CardLabel = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InflationCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    CpiRate = table.Column<double>(type: "REAL", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InflationCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    BudgetStartDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetLimits_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreditCardAccountId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_CreditCardAccounts_CreditCardAccountId",
                        column: x => x.CreditCardAccountId,
                        principalTable: "CreditCardAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "Maaş", 0 },
                    { 2, "Ek Gelir/Serbest", 0 },
                    { 3, "Market & Mutfak", 1 },
                    { 4, "Kira", 1 },
                    { 5, "Faturalar", 1 },
                    { 6, "Eğlence", 1 },
                    { 7, "Kişisel Harçlık", 1 },
                    { 8, "Çocuk Harçlığı", 1 },
                    { 9, "Taze Gıda & Pazar", 1 },
                    { 10, "Aile Harcamaları", 1 },
                    { 11, "Ulaşım", 1 },
                    { 12, "Ev & Yaşam", 1 },
                    { 13, "Giyim", 1 },
                    { 14, "Eğitim", 1 },
                    { 15, "Sağlık", 1 },
                    { 16, "Hediye & Bağış", 1 },
                    { 17, "Yemek Ödeneği", 0 },
                    { 18, "Aile Desteği", 0 },
                    { 19, "Kredi Kartı Çekilen", 0 },
                    { 20, "Ekstra Ödemesi", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLimits_CategoryId",
                table: "BudgetLimits",
                column: "CategoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InflationCaches_Year_Month",
                table: "InflationCaches",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreditCardAccountId",
                table: "Transactions",
                column: "CreditCardAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetLimits");

            migrationBuilder.DropTable(
                name: "InflationCaches");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "CreditCardAccounts");
        }
    }
}
