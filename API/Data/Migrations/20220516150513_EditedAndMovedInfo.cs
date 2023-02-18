using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class EditedAndMovedInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateEdited",
                table: "IssueReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateMoved",
                table: "IssueReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Edited",
                table: "IssueReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Moved",
                table: "IssueReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrignalIssueSourceId",
                table: "IssueReports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_OrignalIssueSourceId",
                table: "IssueReports",
                column: "OrignalIssueSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueReports_IssueStatus_OrignalIssueSourceId",
                table: "IssueReports",
                column: "OrignalIssueSourceId",
                principalTable: "IssueStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueReports_IssueStatus_OrignalIssueSourceId",
                table: "IssueReports");

            migrationBuilder.DropIndex(
                name: "IX_IssueReports_OrignalIssueSourceId",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "DateEdited",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "DateMoved",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "Edited",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "Moved",
                table: "IssueReports");

            migrationBuilder.DropColumn(
                name: "OrignalIssueSourceId",
                table: "IssueReports");
        }
    }
}
