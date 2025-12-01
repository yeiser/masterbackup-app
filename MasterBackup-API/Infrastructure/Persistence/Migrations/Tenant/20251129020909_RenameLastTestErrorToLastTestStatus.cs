using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RenameLastTestErrorToLastTestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTestError",
                table: "DatabaseConnections",
                newName: "LastTestStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastTestStatus",
                table: "DatabaseConnections",
                newName: "LastTestError");
        }
    }
}
