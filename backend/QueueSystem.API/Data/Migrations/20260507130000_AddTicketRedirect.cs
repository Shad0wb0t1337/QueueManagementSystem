using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507130000_AddTicketRedirect")]
    public partial class AddTicketRedirect : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "QueueEnteredAt",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql("UPDATE Tickets SET QueueEnteredAt = CreatedAt;");

            migrationBuilder.CreateTable(
                name: "TicketRedirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromServiceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToServiceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromCounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    RedirectedByOperatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketRedirects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketRedirects_Counters_FromCounterId",
                        column: x => x.FromCounterId,
                        principalTable: "Counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketRedirects_Operators_RedirectedByOperatorId",
                        column: x => x.RedirectedByOperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketRedirects_ServiceTypes_FromServiceTypeId",
                        column: x => x.FromServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketRedirects_ServiceTypes_ToServiceTypeId",
                        column: x => x.ToServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketRedirects_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketRedirects_FromCounterId",
                table: "TicketRedirects",
                column: "FromCounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRedirects_FromServiceTypeId",
                table: "TicketRedirects",
                column: "FromServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRedirects_RedirectedByOperatorId",
                table: "TicketRedirects",
                column: "RedirectedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRedirects_TicketId",
                table: "TicketRedirects",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketRedirects_ToServiceTypeId",
                table: "TicketRedirects",
                column: "ToServiceTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketRedirects");

            migrationBuilder.DropColumn(
                name: "QueueEnteredAt",
                table: "Tickets");
        }
    }
}
