using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddApiKeyLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RemoteIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyLogs_CreatedAt",
                table: "ApiKeyLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyLogs_TenantId",
                table: "ApiKeyLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyLogs_TenantId_CreatedAt",
                table: "ApiKeyLogs",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyLogs");
        }
    }
}
