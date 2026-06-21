using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBesContractDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BesContractNo",
                table: "InvestmentAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BesRetirementDate",
                table: "InvestmentAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BesStartDate",
                table: "InvestmentAssets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BesContractNo",
                table: "InvestmentAssets");

            migrationBuilder.DropColumn(
                name: "BesRetirementDate",
                table: "InvestmentAssets");

            migrationBuilder.DropColumn(
                name: "BesStartDate",
                table: "InvestmentAssets");
        }
    }
}
