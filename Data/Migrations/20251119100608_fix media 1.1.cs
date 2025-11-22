using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trang_tin_điện_tử_mvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class fixmedia11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caption",
                table: "Media");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Media",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Media",
                newName: "FileUrl");

            migrationBuilder.AlterColumn<int>(
                name: "ArticleId",
                table: "Media",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Media",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeKB",
                table: "Media",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Media",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FileSizeKB",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Media");

            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "Media",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Media",
                newName: "UploadedAt");

            migrationBuilder.AlterColumn<int>(
                name: "ArticleId",
                table: "Media",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "Media",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
