using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrainerAthleteLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trainer_athlete_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AthleteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_athlete_assignments", x => x.Id);
                });

            // Backfill: every series assignment already implies the roster link (the trainer is the
            // series author). One assignment per athlete makes this at most one link per athlete,
            // which is why the unique index that follows holds.
            migrationBuilder.Sql(
                """
                INSERT INTO trainer_athlete_assignments ("Id", "TrainerId", "AthleteId", "LinkedAt")
                SELECT a."Id", s."TrainerId", a."UserId", a."AssignedAt"
                FROM workout_series_assignments a
                JOIN workout_series s ON s."Id" = a."WorkoutSeriesId"
                WHERE NOT EXISTS (
                    SELECT 1 FROM trainer_athlete_assignments l
                    WHERE l."AthleteId" = a."UserId" AND l."TrainerId" = s."TrainerId");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_athlete_assignments_AthleteId",
                table: "trainer_athlete_assignments",
                column: "AthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trainer_athlete_assignments_TrainerId",
                table: "trainer_athlete_assignments",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trainer_athlete_assignments");
        }
    }
}
