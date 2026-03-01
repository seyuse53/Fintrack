using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvestmentAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LinkedBankAccountId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_BankAccounts_LinkedBankAccountId",
                        column: x => x.LinkedBankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvestmentTransactions_InvestmentAssets_InvestmentAssetId",
                        column: x => x.InvestmentAssetId,
                        principalTable: "InvestmentAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_InvestmentAssetId",
                table: "InvestmentTransactions",
                column: "InvestmentAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_LinkedBankAccountId",
                table: "InvestmentTransactions",
                column: "LinkedBankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentTransactions");

            migrationBuilder.DropTable(
                name: "InvestmentAssets");
        }
    }
}
