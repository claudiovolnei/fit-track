using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Adapters.Out.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueExerciseName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_exercises_Name",
                table: "exercises",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exercises_Name",
                table: "exercises");
        }
    }
}
