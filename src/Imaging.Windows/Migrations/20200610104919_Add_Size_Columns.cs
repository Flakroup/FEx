using Microsoft.EntityFrameworkCore.Migrations;

namespace FEx.Imaging.Windows.Migrations
{
    public partial class Add_Size_Columns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PixelHeight",
                table: "IndexEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PixelWidth",
                table: "IndexEntries",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PixelHeight",
                table: "IndexEntries");

            migrationBuilder.DropColumn(
                name: "PixelWidth",
                table: "IndexEntries");
        }
    }
}
