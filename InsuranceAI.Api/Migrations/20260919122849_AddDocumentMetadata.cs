using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "DocumentChunks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "DocumentChunks");
        }
    }
}
