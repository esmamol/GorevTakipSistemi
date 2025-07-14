using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorevTakipSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Tasks");
        }
    }
}
