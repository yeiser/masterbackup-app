using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddAutoRestoreToBackupSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackupHistories_JobId",
                table: "BackupHistories");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRestore",
                table: "BackupSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_JobId",
                table: "BackupHistories",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackupHistories_JobId",
                table: "BackupHistories");

            migrationBuilder.DropColumn(
                name: "AutoRestore",
                table: "BackupSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_JobId",
                table: "BackupHistories",
                column: "JobId");
        }
    }
}
