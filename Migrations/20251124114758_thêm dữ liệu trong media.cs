using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trang_tin_điện_tử_mvc.Migrations
{
    /// <inheritdoc />
    public partial class thêmdữliệutrongmedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Media",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserId",
                table: "Media",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_UploadedByUserId",
                table: "Media",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_AspNetUsers_UploadedByUserId",
                table: "Media",
                column: "UploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_AspNetUsers_UploadedByUserId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_UploadedByUserId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "Media");
        }
    }
}
