using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationNameAndMigrationReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MigracionId",
                table: "ConfiguracionEvaluacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "ConfiguracionEvaluacion",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionEvaluacion_MigracionId",
                table: "ConfiguracionEvaluacion",
                column: "MigracionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfiguracionEvaluacion_Migracion_MigracionId",
                table: "ConfiguracionEvaluacion",
                column: "MigracionId",
                principalTable: "Migracion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfiguracionEvaluacion_Migracion_MigracionId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracionEvaluacion_MigracionId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropColumn(
                name: "MigracionId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "ConfiguracionEvaluacion");
        }
    }
}
