using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEvaluationToSingleTestType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionPrueba_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropTable(
                name: "ConfiguracionPrueba");

            migrationBuilder.DropIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropColumn(
                name: "TipoPruebaId",
                table: "ParametroEvaluacion");

            migrationBuilder.AddColumn<int>(
                name: "TipoPruebaId",
                table: "ConfiguracionEvaluacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion",
                column: "ConfiguracionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionEvaluacion_TipoPruebaId",
                table: "ConfiguracionEvaluacion",
                column: "TipoPruebaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfiguracionEvaluacion_TipoPrueba_TipoPruebaId",
                table: "ConfiguracionEvaluacion",
                column: "TipoPruebaId",
                principalTable: "TipoPrueba",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion",
                column: "ConfiguracionId",
                principalTable: "ConfiguracionEvaluacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfiguracionEvaluacion_TipoPrueba_TipoPruebaId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId",
                table: "ParametroEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracionEvaluacion_TipoPruebaId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.DropColumn(
                name: "TipoPruebaId",
                table: "ConfiguracionEvaluacion");

            migrationBuilder.AddColumn<int>(
                name: "TipoPruebaId",
                table: "ParametroEvaluacion",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_ParametroEvaluacion_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion",
                columns: new[] { "ConfiguracionId", "TipoPruebaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionPrueba_TipoPruebaId",
                table: "ConfiguracionPrueba",
                column: "TipoPruebaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParametroEvaluacion_ConfiguracionPrueba_ConfiguracionId_TipoPruebaId",
                table: "ParametroEvaluacion",
                columns: new[] { "ConfiguracionId", "TipoPruebaId" },
                principalTable: "ConfiguracionPrueba",
                principalColumns: new[] { "ConfiguracionId", "TipoPruebaId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
