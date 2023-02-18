using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class StatusApprovalItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Remark",
                table: "StatusUpdates",
                newName: "WorkType");

            migrationBuilder.RenameColumn(
                name: "DateRemarked",
                table: "StatusUpdates",
                newName: "DateReported");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "StatusUpdates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "NewUnit",
                table: "StatusUpdates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonDetails",
                table: "StatusUpdates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleUnit",
                table: "StatusUpdates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusUpdateDetails",
                table: "StatusUpdates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusUpdateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalItem_StatusUpdates_StatusUpdateId",
                        column: x => x.StatusUpdateId,
                        principalTable: "StatusUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalItem_StatusUpdateId",
                table: "ApprovalItem",
                column: "StatusUpdateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalItem");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "StatusUpdates");

            migrationBuilder.DropColumn(
                name: "NewUnit",
                table: "StatusUpdates");

            migrationBuilder.DropColumn(
                name: "ReasonDetails",
                table: "StatusUpdates");

            migrationBuilder.DropColumn(
                name: "ResponsibleUnit",
                table: "StatusUpdates");

            migrationBuilder.DropColumn(
                name: "StatusUpdateDetails",
                table: "StatusUpdates");

            migrationBuilder.RenameColumn(
                name: "WorkType",
                table: "StatusUpdates",
                newName: "Remark");

            migrationBuilder.RenameColumn(
                name: "DateReported",
                table: "StatusUpdates",
                newName: "DateRemarked");
        }
    }
}
