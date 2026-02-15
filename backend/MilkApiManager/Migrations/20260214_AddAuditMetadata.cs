using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MilkApiManager.Migrations
{
    public partial class AddAuditMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogEntries",
                type: "varchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "AuditLogEntries",
                type: "varchar(128)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorIp",
                table: "AuditLogEntries",
                type: "varchar(64)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CorrelationId",
                table: "AuditLogEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_RequestId",
                table: "AuditLogEntries",
                column: "RequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_CorrelationId",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_RequestId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AuditLogEntries");

            migrationBuilder.DropColumn(
                name: "OperatorIp",
                table: "AuditLogEntries");
        }
    }
}
