using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RemoveWorkerTableFromTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatabaseConnections_Worker_AssignedWorkerId",
                table: "DatabaseConnections");

            migrationBuilder.DropTable(
                name: "Worker");

            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropIndex(
                name: "IX_DatabaseConnections_AssignedWorkerId",
                table: "DatabaseConnections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectionString = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentActiveJobs = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Hostname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastJobExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MacAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaxConcurrentJobs = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OsInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SupportedDatabaseTypes = table.Column<string[]>(type: "text[]", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    TotalBackupsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    TotalBytesProcessed = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Worker_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_AssignedWorkerId",
                table: "DatabaseConnections",
                column: "AssignedWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Worker_TenantId",
                table: "Worker",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatabaseConnections_Worker_AssignedWorkerId",
                table: "DatabaseConnections",
                column: "AssignedWorkerId",
                principalTable: "Worker",
                principalColumn: "Id");
        }
    }
}
