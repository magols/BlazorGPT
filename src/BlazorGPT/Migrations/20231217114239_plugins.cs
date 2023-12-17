using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorGPT.Migrations
{
    /// <inheritdoc />
    public partial class plugins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PluginsNames",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PluginsNames",
                table: "Conversations");
        }
    }
}
