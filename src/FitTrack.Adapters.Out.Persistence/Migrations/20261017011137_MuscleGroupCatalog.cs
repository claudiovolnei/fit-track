using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <summary>
    /// Promotes the muscle group from a free-form label on the exercise to its own catalog aggregate.
    /// The label column is dropped only after every exercise has been linked to a group, so no
    /// catalog entry is lost on the way.
    /// </summary>
    public partial class MuscleGroupCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "muscle_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muscle_groups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_muscle_groups_Name",
                table: "muscle_groups",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MuscleGroupId",
                table: "exercises",
                type: "uuid",
                nullable: true);

            // Backfill: one muscle group per distinct label already in the catalog, then every
            // exercise points at the group with its former label. Disposable by design: there is no
            // production data, and the alternative (a default group) would invent a grouping.
            migrationBuilder.Sql(
                """
                INSERT INTO muscle_groups ("Id", "Name")
                SELECT gen_random_uuid(), labels."Name"
                FROM (SELECT DISTINCT btrim("MuscleGroup") AS "Name" FROM exercises) AS labels
                WHERE labels."Name" <> '';

                UPDATE exercises
                SET "MuscleGroupId" = muscle_groups."Id"
                FROM muscle_groups
                WHERE muscle_groups."Name" = btrim(exercises."MuscleGroup");
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "MuscleGroupId",
                table: "exercises",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_MuscleGroupId",
                table: "exercises",
                column: "MuscleGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_exercises_muscle_groups_MuscleGroupId",
                table: "exercises",
                column: "MuscleGroupId",
                principalTable: "muscle_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "MuscleGroup",
                table: "exercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exercises_muscle_groups_MuscleGroupId",
                table: "exercises");

            migrationBuilder.DropIndex(
                name: "IX_exercises_MuscleGroupId",
                table: "exercises");

            migrationBuilder.AddColumn<string>(
                name: "MuscleGroup",
                table: "exercises",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            // Writes the group name back as the label before the aggregate goes away.
            migrationBuilder.Sql(
                """
                UPDATE exercises
                SET "MuscleGroup" = muscle_groups."Name"
                FROM muscle_groups
                WHERE muscle_groups."Id" = exercises."MuscleGroupId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "MuscleGroup",
                table: "exercises",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "MuscleGroupId",
                table: "exercises");

            migrationBuilder.DropTable(
                name: "muscle_groups");
        }
    }
}
