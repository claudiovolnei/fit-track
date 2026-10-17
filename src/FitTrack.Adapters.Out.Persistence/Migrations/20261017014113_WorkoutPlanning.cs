using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkoutPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinishedAt",
                table: "workout_sessions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "workout_sessions",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkoutPlanOptionId",
                table: "workout_sessions",
                type: "uuid",
                nullable: true);

            // Sessions recorded before the timer existed have no start instant. The workout's own date,
            // at midnight UTC, is the closest honest answer: the row keeps a coherent clock instead of
            // the year 0001, and the column can then be made mandatory.
            migrationBuilder.Sql(
                """
                UPDATE workout_sessions
                SET "StartedAt" = "WorkoutDate"::timestamptz
                WHERE "StartedAt" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "workout_sessions",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "workout_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    WorkoutPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "workout_sessions");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanOptionId",
                table: "workout_sessions");
        }
    }
}
