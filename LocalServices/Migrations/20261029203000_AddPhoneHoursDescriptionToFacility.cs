using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalServices.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneHoursDescriptionToFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Facilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Facilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "Facilities",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "Facilities");
        }
    }
}

