using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMessenger.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMessageIsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Messages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
