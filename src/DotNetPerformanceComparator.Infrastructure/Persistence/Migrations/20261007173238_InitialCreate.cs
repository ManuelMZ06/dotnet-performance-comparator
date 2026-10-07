using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Metrica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Unidad = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrica", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoDotNet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    RutaOriginal = table.Column<string>(type: "TEXT", nullable: false),
                    VersionBase = table.Column<string>(type: "TEXT", nullable: false),
                    EstadoValidacion = table.Column<string>(type: "TEXT", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoDotNet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoPrueba",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoPrueba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroEjecuciones = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionEvaluacion_ProyectoDotNet_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "ProyectoDotNet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Migracion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProyectoId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionOrigen = table.Column<string>(type: "TEXT", nullable: false),
                    VersionDestino = table.Column<string>(type: "TEXT", nullable: false),
                    RutaMigrada = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Detalle = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Migracion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Migracion_ProyectoDotNet_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "ProyectoDotNet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionMetrica",
                columns: table => new
                {
                    ConfiguracionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionMetrica", x => new { x.ConfiguracionId, x.MetricaId });
                    table.ForeignKey(
                        name: "FK_ConfiguracionMetrica_ConfiguracionEvaluacion_ConfiguracionId",
                        column: x => x.ConfiguracionId,
                        principalTable: "ConfiguracionEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionMetrica_Metrica_MetricaId",
                        column: x => x.MetricaId,
                        principalTable: "Metrica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionPrueba",
                columns: table => new
                {
                    ConfiguracionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoPruebaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionPrueba", x => new { x.ConfiguracionId, x.TipoPruebaId });
                    table.ForeignKey(
                        name: "FK_ConfiguracionPrueba_ConfiguracionEvaluacion_ConfiguracionId",
                        column: x => x.ConfiguracionId,
                        principalTable: "ConfiguracionEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionPrueba_TipoPrueba_TipoPruebaId",
                        column: x => x.TipoPruebaId,
                        principalTable: "TipoPrueba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ejecucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfiguracionId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionDotNet = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroEjecucion = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejecucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ejecucion_ConfiguracionEvaluacion_ConfiguracionId",
                        column: x => x.ConfiguracionId,
                        principalTable: "ConfiguracionEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParametroEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfiguracionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametroEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParametroEvaluacion_ConfiguracionEvaluacion_ConfiguracionId",
                        column: x => x.ConfiguracionId,
                        principalTable: "ConfiguracionEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CondicionEjecucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EjecucionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Clave = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CondicionEjecucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CondicionEjecucion_Ejecucion_EjecucionId",
                        column: x => x.EjecucionId,
                        principalTable: "Ejecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultadoMetrica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EjecucionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetricaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadoMetrica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadoMetrica_Ejecucion_EjecucionId",
                        column: x => x.EjecucionId,
                        principalTable: "Ejecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadoMetrica_Metrica_MetricaId",
                        column: x => x.MetricaId,
                        principalTable: "Metrica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Metrica",
                columns: new[] { "Id", "Descripcion", "Nombre", "Unidad" },
                values: new object[,]
                {
                    { 1, "Uso de CPU.", "CPU", "%" },
                    { 2, "Consumo de memoria RAM.", "Memoria RAM", "MB" },
                    { 3, "Tiempo de respuesta de las solicitudes.", "Tiempo de respuesta", "ms" },
                    { 4, "Latencia de las solicitudes.", "Latencia", "ms" },
                    { 5, "Solicitudes procesadas por segundo.", "Throughput", "req/s" },
                    { 6, "Tamaño de la aplicación.", "Tamaño de la aplicación", "MB" }
                });

            migrationBuilder.InsertData(
                table: "TipoPrueba",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Evalúa el comportamiento de la aplicación bajo una carga de trabajo esperada.", "Carga" },
                    { 2, "Evalúa el comportamiento de la aplicación bajo una carga superior a la esperada.", "Estrés" },
                    { 3, "Evalúa el comportamiento de la aplicación ante solicitudes simultáneas.", "Concurrencia" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CondicionEjecucion_EjecucionId",
                table: "CondicionEjecucion",
                column: "EjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionEvaluacion_ProyectoId",
                table: "ConfiguracionEvaluacion",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionMetrica_MetricaId",
                table: "ConfiguracionMetrica",
                column: "MetricaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionPrueba_TipoPruebaId",
                table: "ConfiguracionPrueba",
                column: "TipoPruebaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ejecucion_ConfiguracionId",
                table: "Ejecucion",
                column: "ConfiguracionId");

            migrationBuilder.CreateIndex(
                name: "IX_Metrica_Nombre",
                table: "Metrica",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Migracion_ProyectoId",
                table: "Migracion",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion",
                column: "ConfiguracionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetrica_EjecucionId",
                table: "ResultadoMetrica",
                column: "EjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadoMetrica_MetricaId",
                table: "ResultadoMetrica",
                column: "MetricaId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoPrueba_Nombre",
                table: "TipoPrueba",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CondicionEjecucion");

            migrationBuilder.DropTable(
                name: "ConfiguracionMetrica");

            migrationBuilder.DropTable(
                name: "ConfiguracionPrueba");

            migrationBuilder.DropTable(
                name: "Migracion");

            migrationBuilder.DropTable(
                name: "ParametroEvaluacion");

            migrationBuilder.DropTable(
                name: "ResultadoMetrica");

            migrationBuilder.DropTable(
                name: "TipoPrueba");

            migrationBuilder.DropTable(
                name: "Ejecucion");

            migrationBuilder.DropTable(
                name: "Metrica");

            migrationBuilder.DropTable(
                name: "ConfiguracionEvaluacion");

            migrationBuilder.DropTable(
                name: "ProyectoDotNet");
        }
    }
}
