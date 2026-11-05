using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyara.Services.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_AddressId",
                table: "Bookings",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Addresses_AddressId",
                table: "Bookings",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Addresses_AddressId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_AddressId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Bookings");
        }
    }
}
