using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardToInvestments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LinkedCreditCardAccountId",
                table: "InvestmentTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentTransactions_LinkedCreditCardAccountId",
                table: "InvestmentTransactions",
                column: "LinkedCreditCardAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestmentTransactions_CreditCardAccounts_LinkedCreditCardAccountId",
                table: "InvestmentTransactions",
                column: "LinkedCreditCardAccountId",
                principalTable: "CreditCardAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestmentTransactions_CreditCardAccounts_LinkedCreditCardAccountId",
                table: "InvestmentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InvestmentTransactions_LinkedCreditCardAccountId",
                table: "InvestmentTransactions");

            migrationBuilder.DropColumn(
                name: "LinkedCreditCardAccountId",
                table: "InvestmentTransactions");
        }
    }
}
