using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerEMSI.Migrations
{
    /// <inheritdoc />
    public partial class AddPostType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<sbyte>(
                name: "PostType",
                table: "notifications",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostType",
                table: "notifications");
        }
    }
}
