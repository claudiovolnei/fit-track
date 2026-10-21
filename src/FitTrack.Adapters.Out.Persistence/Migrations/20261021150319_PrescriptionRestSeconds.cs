using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrescriptionRestSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "workout_series_workout_exercises",
                type: "integer",
                nullable: false,
                defaultValue: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "workout_series_workout_exercises");
        }
    }
}
