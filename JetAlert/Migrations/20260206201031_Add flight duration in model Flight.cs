using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JetAlert.Migrations
{
    /// <inheritdoc />
    public partial class AddflightdurationinmodelFlight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Flights",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Seats",
                table: "Flights",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "Seats",
                table: "Flights");
        }
    }
}
