using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyara.Services.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameRatingToReviewAndAddProviderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Ratings",
                newName: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Bookings_BookingId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_BookingId",
                table: "Reviews");

            migrationBuilder.RenameIndex(
                name: "PK_Ratings",
                table: "Reviews",
                newName: "PK_Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_Stars",
                table: "Reviews",
                sql: "[Stars] BETWEEN 1 AND 5");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Bookings_BookingId",
                table: "Reviews",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Bookings_BookingId",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_Stars",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Reviews");

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.RenameIndex(
                name: "PK_Reviews",
                table: "Reviews",
                newName: "PK_Ratings");

            migrationBuilder.RenameTable(
                name: "Reviews",
                newName: "Ratings");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Bookings_BookingId",
                table: "Ratings",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_BookingId",
                table: "Ratings",
                column: "BookingId",
                unique: true,
                filter: "[BookingId] IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Services");

        }
    }
}
