using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstaparParkingChallenge.Site.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddParkingSessionGarageSectorFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GarageSectorId",
                table: "ParkingSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_GarageSectorId_ExitTime",
                table: "ParkingSessions",
                columns: new[] { "GarageSectorId", "ExitTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_GarageSectors_GarageSectorId",
                table: "ParkingSessions",
                column: "GarageSectorId",
                principalTable: "GarageSectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_GarageSectors_GarageSectorId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_GarageSectorId_ExitTime",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "GarageSectorId",
                table: "ParkingSessions");
        }
    }
}
