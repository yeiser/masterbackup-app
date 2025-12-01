using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddDatabaseConnectionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Database = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SSLMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTestSuccessful = table.Column<bool>(type: "boolean", nullable: true),
                    LastTestError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NextRun = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupSchedules_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_DatabaseConnectionId",
                table: "BackupSchedules",
                column: "DatabaseConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_IsActive",
                table: "BackupSchedules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_NextRun",
                table: "BackupSchedules",
                column: "NextRun");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_CreatedBy",
                table: "DatabaseConnections",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_IsActive",
                table: "DatabaseConnections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_Name",
                table: "DatabaseConnections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_Type",
                table: "DatabaseConnections",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupSchedules");

            migrationBuilder.DropTable(
                name: "DatabaseConnections");
        }
    }
}
