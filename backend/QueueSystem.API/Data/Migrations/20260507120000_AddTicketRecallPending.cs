using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507120000_AddTicketRecallPending")]
    public partial class AddTicketRecallPending : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecallRequestedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecallRequestedAt",
                table: "Tickets");
        }
    }
}
