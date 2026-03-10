using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstaparParkingChallenge.Site.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RemoveParkingSessionSectorColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_Sector_ExitTime",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "ParkingSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "ParkingSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_Sector_ExitTime",
                table: "ParkingSessions",
                columns: new[] { "Sector", "ExitTime" });
        }
    }
}
