using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Counters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Counters SET Status = CASE WHEN IsActive = 1 THEN 0 ELSE 2 END;");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Counters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Counters",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE Counters SET IsActive = CASE WHEN Status = 0 THEN 1 ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Counters");
        }
    }
}
