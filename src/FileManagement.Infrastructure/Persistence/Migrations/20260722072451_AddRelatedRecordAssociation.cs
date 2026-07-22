using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedRecordAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "related_record_id",
                table: "stored_files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "related_record_type",
                table: "stored_files",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stored_files_related_record",
                table: "stored_files",
                columns: new[] { "related_record_type", "related_record_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stored_files_related_record",
                table: "stored_files");

            migrationBuilder.DropColumn(
                name: "related_record_id",
                table: "stored_files");

            migrationBuilder.DropColumn(
                name: "related_record_type",
                table: "stored_files");
        }
    }
}
