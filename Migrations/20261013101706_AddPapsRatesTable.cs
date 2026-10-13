using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAPPSADMIN.Migrations
{
    /// <inheritdoc />
    public partial class AddPapsRatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PapsRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    FeeRequestId = table.Column<int>(type: "int", nullable: false),
                    WemaPercentage = table.Column<double>(type: "float", nullable: false),
                    WemaMinimumFee = table.Column<double>(type: "float", nullable: false),
                    WemaMaximumFee = table.Column<double>(type: "float", nullable: false),
                    PapsPercentage = table.Column<double>(type: "float", nullable: false),
                    PapsMinimumFee = table.Column<double>(type: "float", nullable: false),
                    PapsMaximumFee = table.Column<double>(type: "float", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PapsRates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PapsRates");
        }
    }
}
