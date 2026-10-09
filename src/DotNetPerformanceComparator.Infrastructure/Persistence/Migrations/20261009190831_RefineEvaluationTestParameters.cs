using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineEvaluationTestParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion");

            migrationBuilder.AddColumn<int>(
                name: "TipoPruebaId",
                table: "ParametroEvaluacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "ConfiguracionEvaluacion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RutaObjetivo",
                table: "ConfiguracionEvaluacion",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion",
                columns: new[] { "ConfiguracionId", "TipoPruebaId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConfiguracionEvaluacion_NumeroEjecuciones",
                table: "ConfiguracionEvaluacion",
                sql: "\"NumeroEjecuciones\" >= 1 AND \"NumeroEjecuciones\" <= 3");

            migrationBuilder.AddForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionPrueba_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion",
                columns: new[] { "ConfiguracionId", "TipoPruebaId" },
                principalTable: "ConfiguracionPrueba",
                principalColumns: new[] { "ConfiguracionId", "TipoPruebaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionPrueba_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConfiguracionEvaluacion_NumeroEjecuciones",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropColumn(
                name: "TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropColumn(
                name: "RutaObjetivo",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.CreateIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion",
                column: "ConfiguracionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion",
                column: "ConfiguracionId",
                principalTable: "ConfiguracionEvaluacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
