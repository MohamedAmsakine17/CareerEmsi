using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerEMSI.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserFieldsNullable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Biography",
                table: "users",
                nullable: true,  // ← Now nullable
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "About",
                table: "users",
                nullable: true,  // ← Now nullable
                oldNullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "users",
                nullable: true,  // ← Now nullable
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
