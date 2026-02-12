using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroSolutions.Propriedades.WebApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Propriedades",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Propriedades");
        }
    }
}
