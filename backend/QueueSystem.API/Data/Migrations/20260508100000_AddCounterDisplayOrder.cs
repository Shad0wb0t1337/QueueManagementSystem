using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260508100000_AddCounterDisplayOrder")]
    public partial class AddCounterDisplayOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Counters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE Counters
                SET DisplayOrder = Id
                WHERE DisplayOrder = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Counters_DisplayOrder",
                table: "Counters",
                column: "DisplayOrder");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Counters_DisplayOrder",
                table: "Counters");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Counters");
        }
    }
}
