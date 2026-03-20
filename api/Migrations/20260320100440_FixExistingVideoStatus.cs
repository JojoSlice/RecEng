using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class FixExistingVideoStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mark all existing videos with a non-empty FilePath as Ready (1).
            // These were created before async processing existed and are already transcoded.
            migrationBuilder.Sql(
                "UPDATE \"Videos\" SET \"Status\" = 1 WHERE \"FilePath\" IS NOT NULL AND \"FilePath\" != '';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Videos\" SET \"Status\" = 0;");
        }
    }
}
