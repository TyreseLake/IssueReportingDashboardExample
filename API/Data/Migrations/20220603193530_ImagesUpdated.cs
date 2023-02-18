using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.Data.Migrations
{
    public partial class ImagesUpdated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Image");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Image",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Path",
                table: "Image");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Image",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
