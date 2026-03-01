using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardStatementDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentDueDay",
                table: "CreditCardAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatementDay",
                table: "CreditCardAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDueDay",
                table: "CreditCardAccounts");

            migrationBuilder.DropColumn(
                name: "StatementDay",
                table: "CreditCardAccounts");
        }
    }
}
