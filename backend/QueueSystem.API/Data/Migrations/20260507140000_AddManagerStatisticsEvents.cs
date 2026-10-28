using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507140000_AddManagerStatisticsEvents")]
    public partial class AddManagerStatisticsEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<int>(type: "INTEGER", nullable: false),
                    TicketNumber = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    OperatorId = table.Column<int>(type: "INTEGER", nullable: true),
                    CounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServiceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromServiceTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ToServiceTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    NewStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketEvents_Counters_CounterId",
                        column: x => x.CounterId,
                        principalTable: "Counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketEvents_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketEvents_ServiceTypes_FromServiceTypeId",
                        column: x => x.FromServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketEvents_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketEvents_ServiceTypes_ToServiceTypeId",
                        column: x => x.ToServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketEvents_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_CounterId",
                table: "TicketEvents",
                column: "CounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_CreatedAt",
                table: "TicketEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_EventType",
                table: "TicketEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_FromServiceTypeId",
                table: "TicketEvents",
                column: "FromServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_OperatorId",
                table: "TicketEvents",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_ServiceTypeId",
                table: "TicketEvents",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_TicketId",
                table: "TicketEvents",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEvents_ToServiceTypeId",
                table: "TicketEvents",
                column: "ToServiceTypeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketEvents");
        }
    }
}
