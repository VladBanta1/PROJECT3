using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatUp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSubmitted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubmitted",
                table: "Restaurants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubmitted",
                table: "Restaurants");
        }
    }
}
