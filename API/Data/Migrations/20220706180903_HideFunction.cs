using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class HideFunction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHiddenItems",
                columns: table => new
                {
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    IssueStatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHiddenItems", x => new { x.AppUserId, x.IssueStatusId });
                    table.ForeignKey(
                        name: "FK_UserHiddenItems_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserHiddenItems_IssueStatus_IssueStatusId",
                        column: x => x.IssueStatusId,
                        principalTable: "IssueStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHiddenItems_IssueStatusId",
                table: "UserHiddenItems",
                column: "IssueStatusId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHiddenItems");
        }
    }
}
