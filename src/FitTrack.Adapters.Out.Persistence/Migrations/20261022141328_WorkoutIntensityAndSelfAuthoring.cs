using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkoutIntensityAndSelfAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Intensity",
                table: "workout_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "workout_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAuthorOwnWorkout",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Intensity",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "CanAuthorOwnWorkout",
                table: "AspNetUsers");
        }
    }
}
