using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeQueuePriorityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFastTrack",
                table: "ServiceTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxWaitMinutes",
                table: "ServiceTypes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityLevel",
                table: "ServiceTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.Sql("UPDATE ServiceTypes SET PriorityLevel = 3, IsFastTrack = 0, MaxWaitMinutes = 45 WHERE Prefix = 'A';");
            migrationBuilder.Sql("UPDATE ServiceTypes SET PriorityLevel = 4, IsFastTrack = 0, MaxWaitMinutes = 60 WHERE Prefix = 'B';");
            migrationBuilder.Sql("UPDATE ServiceTypes SET PriorityLevel = 2, IsFastTrack = 1, MaxWaitMinutes = 30 WHERE Prefix = 'C';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFastTrack",
                table: "ServiceTypes");

            migrationBuilder.DropColumn(
                name: "MaxWaitMinutes",
                table: "ServiceTypes");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "ServiceTypes");
        }
    }
}
