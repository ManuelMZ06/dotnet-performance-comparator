using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStressLevelResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NivelEstres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EjecucionId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroNivel = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuariosObjetivo = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    SolicitudesExitosas = table.Column<int>(type: "INTEGER", nullable: false),
                    SolicitudesFallidas = table.Column<int>(type: "INTEGER", nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelEstres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NivelEstres_Ejecucion_EjecucionId",
                        column: x => x.EjecucionId,
                        principalTable: "Ejecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultadoMetricaNivelEstres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NivelEstresId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadoMetricaNivelEstres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadoMetricaNivelEstres_Metrica_MetricaId",
                        column: x => x.MetricaId,
                        principalTable: "Metrica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadoMetricaNivelEstres_NivelEstres_NivelEstresId",
                        column: x => x.NivelEstresId,
                        principalTable: "NivelEstres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NivelEstres_EjecucionId_NumeroNivel",
                table: "NivelEstres",
                columns: new[] { "EjecucionId", "NumeroNivel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetricaNivelEstres_MetricaId",
                table: "ResultadoMetricaNivelEstres",
                column: "MetricaId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetricaNivelEstres_NivelEstresId_MetricaId",
                table: "ResultadoMetricaNivelEstres",
                columns: new[] { "NivelEstresId", "MetricaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultadoMetricaNivelEstres");

            migrationBuilder.DropTable(
                name: "NivelEstres");
        }
    }
}
