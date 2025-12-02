using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBackupHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackupScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BlobUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BackupSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StackTrace = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompressionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IsInstantBackup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupHistories_BackupSchedules_BackupScheduleId",
                        column: x => x.BackupScheduleId,
                        principalTable: "BackupSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BackupHistories_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_BackupScheduleId",
                table: "BackupHistories",
                column: "BackupScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_CreatedAt",
                table: "BackupHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_DatabaseConnectionId",
                table: "BackupHistories",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_IsInstantBackup",
                table: "BackupHistories",
                column: "IsInstantBackup");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_JobId",
                table: "BackupHistories",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_Status_StartTime",
                table: "BackupHistories",
                columns: new string[] { "Status", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupHistories");
        }
    }
}
