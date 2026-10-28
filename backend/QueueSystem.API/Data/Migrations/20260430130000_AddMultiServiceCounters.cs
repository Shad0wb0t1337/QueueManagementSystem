using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiServiceCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CounterServiceTypes",
                columns: table => new
                {
                    CounterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterServiceTypes", x => new { x.CounterId, x.ServiceTypeId });
                    table.ForeignKey(
                        name: "FK_CounterServiceTypes_Counters_CounterId",
                        column: x => x.CounterId,
                        principalTable: "Counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CounterServiceTypes_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CounterServiceTypes_ServiceTypeId",
                table: "CounterServiceTypes",
                column: "ServiceTypeId");

            migrationBuilder.Sql(
                "INSERT INTO CounterServiceTypes (CounterId, ServiceTypeId) " +
                "SELECT Id, ServiceTypeId FROM Counters;");

            migrationBuilder.DropForeignKey(
                name: "FK_Counters_ServiceTypes_ServiceTypeId",
                table: "Counters");

            migrationBuilder.DropIndex(
                name: "IX_Counters_ServiceTypeId",
                table: "Counters");

            migrationBuilder.DropColumn(
                name: "ServiceTypeId",
                table: "Counters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceTypeId",
                table: "Counters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE Counters " +
                "SET ServiceTypeId = (" +
                "SELECT ServiceTypeId FROM CounterServiceTypes " +
                "WHERE CounterServiceTypes.CounterId = Counters.Id " +
                "ORDER BY ServiceTypeId LIMIT 1);");

            migrationBuilder.DropTable(
                name: "CounterServiceTypes");

            migrationBuilder.CreateIndex(
                name: "IX_Counters_ServiceTypeId",
                table: "Counters",
                column: "ServiceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Counters_ServiceTypes_ServiceTypeId",
                table: "Counters",
                column: "ServiceTypeId",
                principalTable: "ServiceTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
