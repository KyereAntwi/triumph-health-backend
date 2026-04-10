using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triumph.HealthMS.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migrations_0002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthFacility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthFacility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthFacility_HealthOrganizations_TenantId",
                        column: x => x.TenantId,
                        principalTable: "HealthOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthFacility_TenantId",
                table: "HealthFacility",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthFacility");
        }
    }
}
