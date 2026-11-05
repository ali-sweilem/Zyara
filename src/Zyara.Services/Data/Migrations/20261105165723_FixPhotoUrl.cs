//using Microsoft.EntityFrameworkCore.Migrations;

//#nullable disable

//namespace Zyara.Services.Data.Migrations
//{
//    /// <inheritdoc />
//    public partial class FixPhotoUrl : Migration
//    {
//        /// <inheritdoc />
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {

//        }

//        /// <inheritdoc />
//        protected override void Down(MigrationBuilder migrationBuilder)
//        {

//        }
//    }
//}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zyara.Services.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPhotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Users");
        }
    }
}
