using Microsoft.EntityFrameworkCore.Migrations;

namespace FEx.Imaging.Windows.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexEntries",
                columns: table => new
                {
                    AbsoluteUri = table.Column<string>(maxLength: 1024, nullable: false),
                    CheckSum = table.Column<string>(maxLength: 1024, nullable: true),
                    FilePath = table.Column<string>(maxLength: 1024, nullable: true),
                    ResponseContentLength = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexEntries", x => x.AbsoluteUri);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexEntries_FilePath",
                table: "IndexEntries",
                column: "FilePath");
        }

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
                name: "IndexEntries");
    }
}
