using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBackupScheduleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackupSchedules_IsActive",
                table: "BackupSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BackupSchedules_NextRun",
                table: "BackupSchedules");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "BackupSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BackupSchedules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastExecutionError",
                table: "BackupSchedules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastExecutionStatus",
                table: "BackupSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRun",
                table: "BackupSchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "BackupSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "BackupSchedules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnCompletion",
                table: "BackupSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnlyOnFailure",
                table: "BackupSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "BackupSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "BackupSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "BackupSchedules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AddColumn<int>(
                name: "TimeoutMinutes",
                table: "BackupSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "BackupSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_CreatedBy",
                table: "BackupSchedules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_NextRun",
                table: "BackupSchedules",
                column: "NextRun",
                filter: "\"NextRun\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_TenantId_IsActive",
                table: "BackupSchedules",
                columns: new string[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BackupSchedules_CreatedBy",
                table: "BackupSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BackupSchedules_NextRun",
                table: "BackupSchedules");

            migrationBuilder.DropIndex(
                name: "IX_BackupSchedules_TenantId_IsActive",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "LastExecutionError",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "LastExecutionStatus",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "LastRun",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "NotifyOnCompletion",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "NotifyOnlyOnFailure",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "TimeoutMinutes",
                table: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BackupSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_IsActive",
                table: "BackupSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_NextRun",
                table: "BackupSchedules",
                column: "NextRun");
        }
    }
}
