using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterServiceTypeIsEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "CounterServiceTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "CounterServiceTypes");
        }
    }
}
