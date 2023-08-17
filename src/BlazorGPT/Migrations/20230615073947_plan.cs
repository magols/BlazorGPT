using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorGPT.Migrations
{
    /// <inheritdoc />
    public partial class plan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Messages");

            migrationBuilder.AddColumn<string>(
                name: "SKPlan",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SKPlan",
                table: "Conversations");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
