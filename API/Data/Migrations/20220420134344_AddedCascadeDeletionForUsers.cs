using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class AddedCascadeDeletionForUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDistricts_AspNetUsers_UserId",
                table: "UserDistricts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDistricts_Districts_DistrictId",
                table: "UserDistricts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserIssueTypes_AspNetUsers_UserId",
                table: "UserIssueTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserIssueTypes_IssueTypes_IssueTypeId",
                table: "UserIssueTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDistricts_AspNetUsers_UserId",
                table: "UserDistricts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDistricts_Districts_DistrictId",
                table: "UserDistricts",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserIssueTypes_AspNetUsers_UserId",
                table: "UserIssueTypes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserIssueTypes_IssueTypes_IssueTypeId",
                table: "UserIssueTypes",
                column: "IssueTypeId",
                principalTable: "IssueTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDistricts_AspNetUsers_UserId",
                table: "UserDistricts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDistricts_Districts_DistrictId",
                table: "UserDistricts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserIssueTypes_AspNetUsers_UserId",
                table: "UserIssueTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserIssueTypes_IssueTypes_IssueTypeId",
                table: "UserIssueTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDistricts_AspNetUsers_UserId",
                table: "UserDistricts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDistricts_Districts_DistrictId",
                table: "UserDistricts",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserIssueTypes_AspNetUsers_UserId",
                table: "UserIssueTypes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserIssueTypes_IssueTypes_IssueTypeId",
                table: "UserIssueTypes",
                column: "IssueTypeId",
                principalTable: "IssueTypes",
                principalColumn: "Id");
        }
    }
}
