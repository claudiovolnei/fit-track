using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SessionExerciseTicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "completed_exercises",
                columns: table => new
                {
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completed_exercises", x => new { x.WorkoutSessionId, x.ExerciseId });
                    table.ForeignKey(
                        name: "FK_completed_exercises_workout_sessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "completed_exercises");
        }
    }
}
