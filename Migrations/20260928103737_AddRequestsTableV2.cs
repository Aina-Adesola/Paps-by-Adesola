using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TemsGroupProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestsTableV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WemaPercentage = table.Column<double>(type: "float", nullable: false),
                    WemaMinimumFee = table.Column<double>(type: "float", nullable: false),
                    WemaMaximumFee = table.Column<double>(type: "float", nullable: false),
                    PapsPercentage = table.Column<double>(type: "float", nullable: false),
                    PapsMinimumFee = table.Column<double>(type: "float", nullable: false),
                    PapsMaximumFee = table.Column<double>(type: "float", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requests");
        }
    }
}
//log the otp to a logged file 

//    learn how to use a log file and how to use try catch inside services