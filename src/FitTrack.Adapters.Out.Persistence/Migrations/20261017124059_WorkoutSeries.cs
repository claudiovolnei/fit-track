using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkoutSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_plan_option_muscle_groups");

            migrationBuilder.DropTable(
                name: "workout_plan_options");

            migrationBuilder.DropTable(
                name: "workout_plans");

            migrationBuilder.DropIndex(
                name: "IX_workout_sessions_UserId_WorkoutDate_WorkoutPlanOptionId",
                table: "workout_sessions");

            migrationBuilder.RenameColumn(
                name: "WorkoutPlanOptionId",
                table: "workout_sessions",
                newName: "WorkoutSeriesWorkoutId");

            migrationBuilder.CreateTable(
                name: "workout_series",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workout_series_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_series_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_series_assignments_workout_series_WorkoutSeriesId",
                        column: x => x.WorkoutSeriesId,
                        principalTable: "workout_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workout_series_workouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_series_workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_series_workouts_workout_series_WorkoutSeriesId",
                        column: x => x.WorkoutSeriesId,
                        principalTable: "workout_series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_series_workout_exercises",
                columns: table => new
                {
                    WorkoutSeriesWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    LoadKilograms = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_series_workout_exercises", x => new { x.WorkoutSeriesWorkoutId, x.ExerciseId });
                    table.ForeignKey(
                        name: "FK_workout_series_workout_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_series_workout_exercises_workout_series_workouts_Wo~",
                        column: x => x.WorkoutSeriesWorkoutId,
                        principalTable: "workout_series_workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_UserId_WorkoutDate_WorkoutSeriesWorkoutId",
                table: "workout_sessions",
                columns: new[] { "UserId", "WorkoutDate", "WorkoutSeriesWorkoutId" },
                unique: true,
                filter: "\"WorkoutSeriesWorkoutId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_TrainerId_Name",
                table: "workout_series",
                columns: new[] { "TrainerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_assignments_ExpiresAt",
                table: "workout_series_assignments",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_assignments_UserId",
                table: "workout_series_assignments",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_assignments_WorkoutSeriesId",
                table: "workout_series_assignments",
                column: "WorkoutSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_workout_exercises_ExerciseId",
                table: "workout_series_workout_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_series_workouts_WorkoutSeriesId_Position",
                table: "workout_series_workouts",
                columns: new[] { "WorkoutSeriesId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_series_assignments");

            migrationBuilder.DropTable(
                name: "workout_series_workout_exercises");

            migrationBuilder.DropTable(
                name: "workout_series_workouts");

            migrationBuilder.DropTable(
                name: "workout_series");

            migrationBuilder.DropIndex(
                name: "IX_workout_sessions_UserId_WorkoutDate_WorkoutSeriesWorkoutId",
                table: "workout_sessions");

            migrationBuilder.RenameColumn(
                name: "WorkoutSeriesWorkoutId",
                table: "workout_sessions",
                newName: "WorkoutPlanOptionId");

            migrationBuilder.CreateTable(
                name: "workout_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workout_plan_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    WorkoutPlanId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plan_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_plan_options_workout_plans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "workout_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_plan_option_muscle_groups",
                columns: table => new
                {
                    WorkoutPlanOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MuscleGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plan_option_muscle_groups", x => new { x.WorkoutPlanOptionId, x.MuscleGroupId });
                    table.ForeignKey(
                        name: "FK_workout_plan_option_muscle_groups_muscle_groups_MuscleGroup~",
                        column: x => x.MuscleGroupId,
                        principalTable: "muscle_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_plan_option_muscle_groups_workout_plan_options_Work~",
                        column: x => x.WorkoutPlanOptionId,
                        principalTable: "workout_plan_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_UserId_WorkoutDate_WorkoutPlanOptionId",
                table: "workout_sessions",
                columns: new[] { "UserId", "WorkoutDate", "WorkoutPlanOptionId" },
                unique: true,
                filter: "\"WorkoutPlanOptionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workout_plan_option_muscle_groups_MuscleGroupId",
                table: "workout_plan_option_muscle_groups",
                column: "MuscleGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_plan_options_WorkoutPlanId_Position",
                table: "workout_plan_options",
                columns: new[] { "WorkoutPlanId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_plans_TrainerId",
                table: "workout_plans",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_plans_UserId",
                table: "workout_plans",
                column: "UserId",
                unique: true);
        }
    }
}
