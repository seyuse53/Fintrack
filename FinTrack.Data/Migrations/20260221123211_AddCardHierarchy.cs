using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCardHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCardId",
                table: "CreditCardAccounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardAccounts_ParentCardId",
                table: "CreditCardAccounts",
                column: "ParentCardId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCardAccounts_CreditCardAccounts_ParentCardId",
                table: "CreditCardAccounts",
                column: "ParentCardId",
                principalTable: "CreditCardAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditCardAccounts_CreditCardAccounts_ParentCardId",
                table: "CreditCardAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CreditCardAccounts_ParentCardId",
                table: "CreditCardAccounts");

            migrationBuilder.DropColumn(
                name: "ParentCardId",
                table: "CreditCardAccounts");
        }
    }
}
