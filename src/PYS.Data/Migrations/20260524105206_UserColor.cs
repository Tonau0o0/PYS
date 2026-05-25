using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PYS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Users",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "#2196F3");

            // Backfill: deterministic default color per user (palette of 12)
            migrationBuilder.Sql(@"
                WITH palette(idx, hex) AS (
                    SELECT * FROM (VALUES
                        (0, '#2196F3'),(1, '#F44336'),(2, '#4CAF50'),(3, '#FF9800'),
                        (4, '#9C27B0'),(5, '#E91E63'),(6, '#00BCD4'),(7, '#FFC107'),
                        (8, '#3F51B5'),(9, '#009688'),(10,'#FF5722'),(11,'#795548')
                    ) AS v(idx, hex)
                )
                UPDATE u
                SET u.ColorHex = p.hex
                FROM Users u
                CROSS APPLY (SELECT TOP 1 hex FROM palette WHERE idx = ABS(u.Id) % 12) p;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Users");
        }
    }
}
