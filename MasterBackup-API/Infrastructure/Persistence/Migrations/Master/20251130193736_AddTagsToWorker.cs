using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddTagsToWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "Workers",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Workers");
        }
    }
}
