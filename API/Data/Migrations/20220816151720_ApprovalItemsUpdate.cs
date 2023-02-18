using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class ApprovalItemsUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalItem_StatusUpdates_StatusUpdateId",
                table: "ApprovalItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApprovalItem",
                table: "ApprovalItem");

            migrationBuilder.RenameTable(
                name: "ApprovalItem",
                newName: "ApprovalItems");

            migrationBuilder.RenameIndex(
                name: "IX_ApprovalItem_StatusUpdateId",
                table: "ApprovalItems",
                newName: "IX_ApprovalItems_StatusUpdateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApprovalItems",
                table: "ApprovalItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalItems_StatusUpdates_StatusUpdateId",
                table: "ApprovalItems",
                column: "StatusUpdateId",
                principalTable: "StatusUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalItems_StatusUpdates_StatusUpdateId",
                table: "ApprovalItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApprovalItems",
                table: "ApprovalItems");

            migrationBuilder.RenameTable(
                name: "ApprovalItems",
                newName: "ApprovalItem");

            migrationBuilder.RenameIndex(
                name: "IX_ApprovalItems_StatusUpdateId",
                table: "ApprovalItem",
                newName: "IX_ApprovalItem_StatusUpdateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApprovalItem",
                table: "ApprovalItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalItem_StatusUpdates_StatusUpdateId",
                table: "ApprovalItem",
                column: "StatusUpdateId",
                principalTable: "StatusUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
