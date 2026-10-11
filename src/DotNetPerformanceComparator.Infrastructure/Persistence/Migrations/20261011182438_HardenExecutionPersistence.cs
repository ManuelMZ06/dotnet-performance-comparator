using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenExecutionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResultadoMetrica_EjecucionId",
                table: "ResultadoMetrica");

            migrationBuilder.DropIndex(
                name: "IX_Ejecucion_ConfiguracionId",
                table: "Ejecucion");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetrica_EjecucionId_MetricaId",
                table: "ResultadoMetrica",
                columns: new[] { "EjecucionId", "MetricaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejecucion_ConfiguracionId_VersionDotNet_NumeroEjecucion",
                table: "Ejecucion",
                columns: new[] { "ConfiguracionId", "VersionDotNet", "NumeroEjecucion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResultadoMetrica_EjecucionId_MetricaId",
                table: "ResultadoMetrica");

            migrationBuilder.DropIndex(
                name: "IX_Ejecucion_ConfiguracionId_VersionDotNet_NumeroEjecucion",
                table: "Ejecucion");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetrica_EjecucionId",
                table: "ResultadoMetrica",
                column: "EjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ejecucion_ConfiguracionId",
                table: "Ejecucion",
                column: "ConfiguracionId");
        }
    }
}
