using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trang_tin_điện_tử_mvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArticleImagePositionsWithRestrictDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleImagePositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    MediaId = table.Column<int>(type: "int", nullable: false),
                    PositionIndex = table.Column<int>(type: "int", nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleImagePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleImagePositions_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleImagePositions_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleImagePositions_ArticleId",
                table: "ArticleImagePositions",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleImagePositions_MediaId",
                table: "ArticleImagePositions",
                column: "MediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleImagePositions");
        }
    }
}
