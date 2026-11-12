using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyara.Services.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotDurationMinutesToTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "TimeSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                table: "TimeSlots");
        }
    }
}
