using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddWorkersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Hostname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OsInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastJobExecution = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupportedDatabaseTypes = table.Column<string[]>(type: "text[]", nullable: false),
                    MaxConcurrentJobs = table.Column<int>(type: "integer", nullable: false),
                    CurrentActiveJobs = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalBackupsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    TotalBytesProcessed = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_LastHeartbeat",
                table: "Workers",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Status",
                table: "Workers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_TenantId",
                table: "Workers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_TenantId_Name",
                table: "Workers",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workers");
        }
    }
}
