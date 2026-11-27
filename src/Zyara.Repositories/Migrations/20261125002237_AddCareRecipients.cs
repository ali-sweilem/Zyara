using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyara.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddCareRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ChronicConditions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareRecipients", x => x.Id);
                    table.CheckConstraint("CK_CareRecipients_Slot", "[Slot] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_CareRecipients_Users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "CareRecipientId",
                table: "Bookings",
                type: "int",
                nullable: true);

            // Backfill: for every account holder with at least one booking, insert a Self CareRecipient
            // (name/phone/gender/DOB from the Users row, medical text fields = 'Not provided', Slot = 1)
            // and set Booking.CareRecipientId on that account's existing bookings.
            migrationBuilder.Sql(@"
                INSERT INTO [CareRecipients] ([PatientId], [Slot], [IsArchived], [FullName], [PhoneNumber], [DateOfBirth], [Gender], [Relationship], [BloodType], [ChronicConditions], [Allergies], [CurrentMedications], [Notes], [CreatedAt])
                SELECT u.[Id], 1, 0, u.[FullName], u.[PhoneNumber], u.[DateOfBirth], u.[Gender], 'Self', 'Unknown', 'Not provided', 'Not provided', 'Not provided', NULL, GETUTCDATE()
                FROM [Users] u
                WHERE u.[Id] IN (SELECT DISTINCT [PatientId] FROM [Bookings])
                  AND NOT EXISTS (SELECT 1 FROM [CareRecipients] cr WHERE cr.[PatientId] = u.[Id] AND cr.[Relationship] = 'Self' AND cr.[IsArchived] = 0);

                UPDATE b
                SET b.[CareRecipientId] = cr.[Id]
                FROM [Bookings] b
                INNER JOIN [CareRecipients] cr ON cr.[PatientId] = b.[PatientId] AND cr.[Relationship] = 'Self' AND cr.[IsArchived] = 0
                WHERE b.[CareRecipientId] IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "CareRecipientId",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CareRecipientId",
                table: "Bookings",
                column: "CareRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_CareRecipients_PatientId",
                table: "CareRecipients",
                column: "PatientId",
                unique: true,
                filter: "[Relationship] = 'Self' AND [IsArchived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CareRecipients_PatientId_Slot",
                table: "CareRecipients",
                columns: new[] { "PatientId", "Slot" },
                unique: true,
                filter: "[IsArchived] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_CareRecipients_CareRecipientId",
                table: "Bookings",
                column: "CareRecipientId",
                principalTable: "CareRecipients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_CareRecipients_CareRecipientId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "CareRecipients");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CareRecipientId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CareRecipientId",
                table: "Bookings");
        }
    }
}
