using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeToInvestments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "InvestmentTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fee",
                table: "InvestmentTransactions");
        }
    }
}
