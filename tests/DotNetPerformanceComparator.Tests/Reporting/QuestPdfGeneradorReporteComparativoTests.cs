using System.Text;
using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Reporting;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Infrastructure.Reporting;

namespace DotNetPerformanceComparator.Tests.Reporting;

/// <summary>
/// Pruebas del generador PDF de CU-09. No comprueban píxeles: verifican que la generación produce un PDF
/// válido en memoria para cada escenario de datos (incluyendo parciales, no comparables y Estrés), sin
/// excepciones. Los valores/metodología provienen del motor real de CU-07.
/// </summary>
public class QuestPdfGeneradorReporteComparativoTests
{
    static QuestPdfGeneradorReporteComparativoTests() =>
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    private const int Cpu = 1, Ram = 2, Lat = 4, Thr = 5, Tam = 6;
    private const string Net8 = VersionesDotNet.Net8;
    private const string Net10 = VersionesDotNet.Net10;

    private static readonly ComparacionResultadosService Comparacion = new();
    private static readonly QuestPdfGeneradorReporteComparativo Generador = new();

    private static ResultadoMetricaConsulta R(int id, decimal valor, string nombre = "M", string unidad = "u") => new(id, nombre, unidad, valor);
    private static MetricaSeleccionadaConsulta M(int id, string nombre, string unidad) => new(id, nombre, unidad);

    private static NivelEstresConsulta Niv(int usuarios, string estado, params ResultadoMetricaConsulta[] resultados) =>
        new(usuarios, usuarios, estado, 10, 0, null, resultados);

    private static EjecucionConsulta Eje(string version, int rep, string estado,
        ResultadoMetricaConsulta[]? resultados = null, NivelEstresConsulta[]? niveles = null, CondicionConsulta[]? condiciones = null) =>
        new(version, rep, estado, DateTime.UtcNow, DateTime.UtcNow, condiciones ?? [], resultados ?? [], niveles ?? []);

    private static EvaluacionDetalleConsulta Detalle(int id, string tipo, MetricaSeleccionadaConsulta[] metricas,
        ParametrosPruebaConsulta? parametros, params EjecucionConsulta[] ejecuciones) =>
        new(id, $"Eval {id}", "App", ".NET 8 → .NET 10 (Exitosa)", tipo, "Finalizada", ejecuciones.Length, "/", "Obs.",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), metricas, ejecuciones, parametros);

    private static ReporteComparativoOpciones Opciones(bool graficos = true, string? observaciones = null) =>
        new("Reporte de prueba", true, true, true, true, graficos, true, observaciones, new DateTime(2026, 10, 15, 14, 25, 30));

    private static ParametrosCargaConsulta ParamsCarga => new(50, 30, 5, 10, 100);

    private static ReportePdfGenerado Generar(EvaluacionDetalleConsulta detalle, ReporteComparativoOpciones? opciones = null)
    {
        var resumen = Comparacion.CompararResumen(detalle);
        var estres = detalle.TipoPrueba == TiposPruebaConocidos.Estres ? Comparacion.CompararEstres(detalle) : null;
        return Generador.Generar(detalle, resumen, estres, opciones ?? Opciones());
    }

    private static void AssertPdfValido(ReportePdfGenerado pdf, int id)
    {
        Assert.NotNull(pdf.Contenido);
        Assert.True(pdf.Contenido.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf.Contenido, 0, 4)); // cabecera PDF
        Assert.StartsWith($"comparacion-evaluacion-{id}-", pdf.NombreArchivo);
        Assert.EndsWith(".pdf", pdf.NombreArchivo);
        Assert.DoesNotContain(' ', pdf.NombreArchivo);
        Assert.Equal($"comparacion-evaluacion-{id}-20261015-142530.pdf", pdf.NombreArchivo); // timestamp = FechaGeneracion
    }

    private static EvaluacionDetalleConsulta CargaSeis(int id = 1) => Detalle(id, TiposPruebaConocidos.Carga,
        [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB"), M(4, "Tiempo de respuesta", "ms"), M(Lat, "Latencia", "ms"), M(Thr, "Throughput", "req/s"), M(Tam, "Tamaño de la aplicación", "MB")],
        ParamsCarga,
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Ram, 80m), R(4, 0.6m), R(Lat, 0.9m), R(Thr, 47m), R(Tam, 8m)], condiciones: [new("SdkVersion", "8.0.425"), new("TargetFramework", "net8.0"), new("ConfiguracionCompilacion", "Release"), new("Environment", "Production")]),
        Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Ram, 78m), R(4, 0.57m), R(Lat, 0.77m), R(Thr, 48m), R(Tam, 11m)]));

    // ------------------------------------------------------------------ normal

    [Fact]
    public void CargaCompleta_GeneraPdfValido()
    {
        AssertPdfValido(Generar(CargaSeis(6)), 6);
    }

    [Fact]
    public void BaseCeroYPorcentajeNull_NoFalla()
    {
        var d = Detalle(1, TiposPruebaConocidos.Carga, [M(Thr, "Throughput", "req/s")], ParamsCarga,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Thr, 0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Thr, 5m)]));
        AssertPdfValido(Generar(d), 1);
    }

    [Fact]
    public void MetricaNoComparableYParcial_NoFalla()
    {
        var d = Detalle(2, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Lat, "Latencia", "ms")], ParamsCarga,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Lat, 1m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m)])); // Latencia no comparable
        AssertPdfValido(Generar(d), 2);
    }

    [Fact]
    public void TotalmenteNoComparable_GeneraPdfInformativo()
    {
        var d = Detalle(3, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")], ParamsCarga,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)])); // solo .NET 8
        AssertPdfValido(Generar(d), 3); // se genera igual (informativo)
    }

    [Fact]
    public void FallidaYCancelada_NoFalla()
    {
        var d = Detalle(4, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")], ParamsCarga,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)]),
            Eje(Net10, 1, EstadosEjecucion.Fallida),
            Eje(Net8, 2, EstadosEjecucion.Cancelada));
        AssertPdfValido(Generar(d), 4);
    }

    [Fact]
    public void SinGraficos_NoFalla()
    {
        AssertPdfValido(Generar(CargaSeis(5), Opciones(graficos: false)), 5);
    }

    [Fact]
    public void ObservacionesLargas_NoFalla()
    {
        var obs = new string('x', 2000);
        AssertPdfValido(Generar(CargaSeis(7), Opciones(observaciones: obs)), 7);
    }

    [Fact]
    public void SeccionesOmitidas_NoFalla()
    {
        var soloDatos = new ReporteComparativoOpciones("Solo datos", true, false, false, false, false, false, null, new DateTime(2026, 10, 15, 14, 25, 30));
        AssertPdfValido(Generar(CargaSeis(8), soloDatos), 8);
    }

    [Fact]
    public void ParametrosAusentes_NoFalla()
    {
        // Parámetros null (configuración histórica/inconsistente): el PDF debe generarse mostrando "No disponible".
        var d = Detalle(13, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")],
            new ParametrosCargaConsulta(null, null, null, null, null),
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m)]));
        AssertPdfValido(Generar(d), 13);
    }

    // ------------------------------------------------------------------ Estrés

    private static EvaluacionDetalleConsulta EstresConMaximo(int id = 9) => Detalle(id, TiposPruebaConocidos.Estres,
        [M(Cpu, "CPU", "%"), M(Tam, "Tamaño de la aplicación", "MB")], new ParametrosEstresConsulta(5, 5, 15, 15, 3, 50),
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.6m), R(Tam, 8m)], [Niv(5, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.55m)), Niv(15, EstadosNivelEstres.Completado, R(Cpu, 0.6m))]),
        Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Tam, 11m)], [Niv(5, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.45m)), Niv(15, EstadosNivelEstres.Completado, R(Cpu, 0.5m))]));

    [Fact]
    public void EstresResumenComparableYNiveles_NoFalla()
    {
        AssertPdfValido(Generar(EstresConMaximo(9)), 9);
    }

    [Fact]
    public void EstresResumenNoDisponiblePorSaturacion_NoFalla()
    {
        var d = Detalle(10, TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")], new ParametrosEstresConsulta(5, 5, 20, 15, 3, 50),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(5, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(10, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(5, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(10, EstadosNivelEstres.Saturado)]));
        AssertPdfValido(Generar(d), 10); // niveles comparables (5) existen; resumen no disponible
    }

    [Fact]
    public void EstresNivelesParcialesYNoComparables_SaturacionVariable_NoFalla()
    {
        var d = Detalle(11, TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")], new ParametrosEstresConsulta(10, 10, 40, 15, 3, 50),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m), R(Ram, 70m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.6m)), Niv(30, EstadosNivelEstres.Saturado)]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(40, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m), R(Ram, 68m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(30, EstadosNivelEstres.Completado, R(Cpu, 0.7m)), Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.8m))]),
            Eje(Net10, 2, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.45m))]));
        AssertPdfValido(Generar(d), 11);
    }

    [Fact]
    public void Concurrencia_NoFalla()
    {
        var d = Detalle(12, TiposPruebaConocidos.Concurrencia, [M(Cpu, "CPU", "%"), M(Thr, "Throughput", "req/s")],
            new ParametrosConcurrenciaConsulta(10, 4, 2, 500),
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Thr, 16m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.42m), R(Thr, 17m)]));
        AssertPdfValido(Generar(d), 12);
    }
}
