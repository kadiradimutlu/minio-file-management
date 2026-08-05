using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyFileOperationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_file_operation_reports",
                columns: table => new
                {
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    uploaded_count = table.Column<int>(type: "integer", nullable: false),
                    downloaded_count = table.Column<int>(type: "integer", nullable: false),
                    deleted_count = table.Column<int>(type: "integer", nullable: false),
                    uploaded_bytes = table.Column<long>(type: "bigint", nullable: false),
                    downloaded_bytes = table.Column<long>(type: "bigint", nullable: false),
                    pending_outbox_count = table.Column<int>(type: "integer", nullable: false),
                    failed_outbox_count = table.Column<int>(type: "integer", nullable: false),
                    invalid_event_count = table.Column<int>(type: "integer", nullable: false),
                    content_type_breakdown = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_file_operation_reports", x => x.report_date);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_file_operation_reports");
        }
    }
}
