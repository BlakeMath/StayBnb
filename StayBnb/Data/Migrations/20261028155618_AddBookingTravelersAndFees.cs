using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StayBnb.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingTravelersAndFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CleaningFee",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfTravelers",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleaningFee",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NumberOfTravelers",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "Bookings");
        }
    }
}
