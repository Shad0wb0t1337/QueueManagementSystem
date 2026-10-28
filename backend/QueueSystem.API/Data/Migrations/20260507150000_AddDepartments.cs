using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueSystem.API.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507150000_AddDepartments")]
    public partial class AddDepartments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO Departments (Id, Name, Description, IsActive, CreatedAt)
                VALUES (1, 'Registratūros skyrius', 'Bendrosios registracijos ir pacientų priėmimo langeliai.', 1, '2026-05-07 00:00:00');
                """);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Counters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ManagerDepartments",
                columns: table => new
                {
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerDepartments", x => new { x.AppUserId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_ManagerDepartments_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManagerDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counters_DepartmentId",
                table: "Counters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerDepartments_DepartmentId",
                table: "ManagerDepartments",
                column: "DepartmentId");

            // SQLite cannot add a foreign key constraint to an existing table with ALTER TABLE.
            // The model still defines the relationship; existing local databases keep the
            // backfilled DepartmentId column and index without an in-place FK rebuild.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagerDepartments");

            migrationBuilder.DropIndex(
                name: "IX_Counters_DepartmentId",
                table: "Counters");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Counters");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
