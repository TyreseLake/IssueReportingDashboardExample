using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class RemovedOldStatusAndRemarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Remark");

            migrationBuilder.DropColumn(
                name: "ReviewDate",
                table: "IssueStatus");

            migrationBuilder.DropColumn(
                name: "ReviewType",
                table: "IssueStatus");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IssueStatus");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDate",
                table: "IssueStatus",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReviewType",
                table: "IssueStatus",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "IssueStatus",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Remark",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    DateRemarked = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssueStatusId = table.Column<int>(type: "int", nullable: true),
                    RemarkData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemarkType = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remark", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remark_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Remark_IssueStatus_IssueStatusId",
                        column: x => x.IssueStatusId,
                        principalTable: "IssueStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Remark_AppUserId",
                table: "Remark",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Remark_IssueStatusId",
                table: "Remark",
                column: "IssueStatusId");
        }
    }
}
