using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstaparParkingChallenge.Site.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarageSectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sector = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarageSectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntryTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExitTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EntryPriceMultiplier = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    BasePriceAtEntry = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountCharged = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpotId = table.Column<int>(type: "int", nullable: true),
                    IsParked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventTypeCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GarageSpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GarageSectorId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    OccupiedByLicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarageSpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GarageSpots_GarageSectors_GarageSectorId",
                        column: x => x.GarageSectorId,
                        principalTable: "GarageSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarageSectors_Sector",
                table: "GarageSectors",
                column: "Sector",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GarageSpots_GarageSectorId",
                table: "GarageSpots",
                column: "GarageSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_GarageSpots_Latitude_Longitude",
                table: "GarageSpots",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_LicensePlate_ExitTime",
                table: "ParkingSessions",
                columns: new[] { "LicensePlate", "ExitTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_Sector_ExitTime",
                table: "ParkingSessions",
                columns: new[] { "Sector", "ExitTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingWebhookEvents_IdempotencyKey",
                table: "ParkingWebhookEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingWebhookEvents_LicensePlate",
                table: "ParkingWebhookEvents",
                column: "LicensePlate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarageSpots");

            migrationBuilder.DropTable(
                name: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "ParkingWebhookEvents");

            migrationBuilder.DropTable(
                name: "GarageSectors");
        }
    }
}
