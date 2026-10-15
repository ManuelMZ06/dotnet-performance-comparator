using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Reporting;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

/// <summary>
/// Pruebas del presentador de M-08 (CU-09): listado, preselección, opciones, validación, generación e
/// invalidación del PDF. Usa un <see cref="IConsultaResultadosService"/> y un generador falsos; el filtro de
/// CU-06 nunca participa (el reporte es del experimento completo).
/// </summary>
public class ReporteComparativoModeloTests
{
    private const int Cpu = 1;
    private const string Net8 = VersionesDotNet.Net8;
    private const string Net10 = VersionesDotNet.Net10;

    private sealed class FakeConsulta : IConsultaResultadosService
    {
        public IReadOnlyList<EvaluacionResumenConsulta> Lista { get; set; } = [];
        public Dictionary<int, EvaluacionDetalleConsulta?> Detalles { get; } = new();

        public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken ct = default) => Task.FromResult(Lista);
        public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int id, CancellationToken ct = default) => Task.FromResult(Detalles.GetValueOrDefault(id));
    }

    private sealed class FakeGenerador : IGeneradorReporteComparativo
    {
        public EvaluacionDetalleConsulta? UltimoDetalle { get; private set; }
        public Exception? Lanzar { get; set; }

        public ReportePdfGenerado Generar(EvaluacionDetalleConsulta detalle, ComparacionResumen comparacion, ComparacionEstres? estres, ReporteComparativoOpciones opciones)
        {
            if (Lanzar is not null) throw Lanzar;
            UltimoDetalle = detalle;
            return new ReportePdfGenerado($"comparacion-evaluacion-{detalle.Id}-20261015-142530.pdf", [1, 2, 3, 4]);
        }
    }

    private static ResultadoMetricaConsulta R(int id, decimal valor, string nombre = "M", string unidad = "u") => new(id, nombre, unidad, valor);
    private static MetricaSeleccionadaConsulta M(int id, string nombre, string unidad) => new(id, nombre, unidad);

    private static EjecucionConsulta Eje(string version, string estado, ResultadoMetricaConsulta[]? resultados = null) =>
        new(version, 1, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], []);

    private static EvaluacionResumenConsulta Resumen(int id) =>
        new(id, $"Eval {id}", "App", "Carga", "Finalizada", 2, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true);

    private static EvaluacionDetalleConsulta DetalleComparable(int id) =>
        new(id, $"Eval {id}", "App", null, TiposPruebaConocidos.Carga, "Finalizada", 2, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), [M(Cpu, "CPU", "%")],
            [Eje(Net8, EstadosEjecucion.Completada, [R(Cpu, 0.5m)]), Eje(Net10, EstadosEjecucion.Completada, [R(Cpu, 0.4m)])]);

    private static EvaluacionDetalleConsulta DetalleNoComparable(int id) =>
        new(id, $"Eval {id}", "App", null, TiposPruebaConocidos.Carga, "Finalizada", 2, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), [M(Cpu, "CPU", "%")],
            [Eje(Net8, EstadosEjecucion.Completada, [R(Cpu, 0.5m)])]);

    private static (ReporteComparativoModelo, FakeConsulta, FakeGenerador) Crear()
    {
        var consulta = new FakeConsulta();
        var generador = new FakeGenerador();
        var modelo = new ReporteComparativoModelo(consulta, new ComparacionResultadosService(), generador, NullLogger.Instance);
        return (modelo, consulta, generador);
    }

    private static async Task<(ReporteComparativoModelo, FakeGenerador)> Preparada(int id = 1, bool comparable = true)
    {
        var (modelo, consulta, generador) = Crear();
        consulta.Lista = [Resumen(id)];
        consulta.Detalles[id] = comparable ? DetalleComparable(id) : DetalleNoComparable(id);
        await modelo.CargarListadoAsync(id);
        return (modelo, generador);
    }

    // ------------------------------------------------------------------ carga / selección

    [Fact]
    public async Task CargarListado_ExponeEvaluaciones()
    {
        var (modelo, consulta, _) = Crear();
        consulta.Lista = [Resumen(1), Resumen(2)];
        await modelo.CargarListadoAsync(null);
        Assert.Equal(2, modelo.Evaluaciones.Count);
        Assert.Null(modelo.SeleccionId); // acceso directo /reportes: sin preselección
    }

    [Fact]
    public async Task Preseleccion_Valida_CargaDetalleYComparacion()
    {
        var (modelo, _) = await Preparada(5);
        Assert.Equal(5, modelo.SeleccionId);
        Assert.NotNull(modelo.Detalle);
        Assert.NotNull(modelo.Resumen);
    }

    [Fact]
    public async Task Preseleccion_Inexistente_Segura()
    {
        var (modelo, consulta, _) = Crear();
        consulta.Lista = [Resumen(1)];
        await modelo.CargarListadoAsync(99); // no está en la lista
        Assert.Null(modelo.SeleccionId);
        Assert.Null(modelo.Detalle);
    }

    [Fact]
    public async Task TituloDefault_AlSeleccionar()
    {
        var (modelo, _) = await Preparada(1);
        Assert.Equal("Reporte comparativo - Eval 1", modelo.Titulo);
    }

    [Fact]
    public async Task SeccionesActivasPorDefecto()
    {
        var (modelo, _) = await Preparada();
        Assert.True(modelo.IncluirDatosGenerales && modelo.IncluirResultadosPorEjecucion && modelo.IncluirPromediosConsolidado
            && modelo.IncluirTablaComparativa && modelo.IncluirGraficos && modelo.IncluirConclusionObservaciones);
        Assert.True(modelo.AlMenosUnaSeccion);
    }

    // ------------------------------------------------------------------ validación

    [Fact]
    public async Task TituloVacio_Invalido()
    {
        var (modelo, _) = await Preparada();
        modelo.EstablecerTitulo("   ");
        Assert.False(modelo.TituloValido);
        Assert.False(modelo.PuedeGenerar);
    }

    [Fact]
    public async Task TodasSeccionesDesactivadas_Invalido()
    {
        var (modelo, _) = await Preparada();
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirDatosGenerales), false);
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirResultadosPorEjecucion), false);
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirPromediosConsolidado), false);
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirTablaComparativa), false);
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirGraficos), false);
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirConclusionObservaciones), false);
        Assert.False(modelo.AlMenosUnaSeccion);
        Assert.False(modelo.PuedeGenerar);
    }

    [Fact]
    public async Task ObservacionesDentroDelLimite_Valido()
    {
        var (modelo, _) = await Preparada();
        modelo.EstablecerObservaciones(new string('x', ReporteComparativoModelo.ObservacionesMaximo));
        Assert.True(modelo.ObservacionesValidas);
        modelo.EstablecerObservaciones(new string('x', ReporteComparativoModelo.ObservacionesMaximo + 1));
        Assert.False(modelo.ObservacionesValidas);
    }

    // ------------------------------------------------------------------ generación / invalidación

    [Fact]
    public async Task Generar_LlamaAlGeneradorConLaEvaluacion_HabilitaDescargar()
    {
        var (modelo, generador) = await Preparada(7);
        modelo.GenerarPdf();
        Assert.Equal(7, generador.UltimoDetalle!.Id);
        Assert.True(modelo.PuedeDescargar);
        Assert.Equal(OperationMessageKind.Success, modelo.MensajeTipo);
    }

    [Fact]
    public async Task CambiarTitulo_InvalidaPdf()
    {
        var (modelo, _) = await Preparada();
        modelo.GenerarPdf();
        Assert.True(modelo.PuedeDescargar);
        modelo.EstablecerTitulo("Otro título");
        Assert.False(modelo.PuedeDescargar);
    }

    [Fact]
    public async Task CambiarCheckbox_InvalidaPdf()
    {
        var (modelo, _) = await Preparada();
        modelo.GenerarPdf();
        modelo.AlternarSeccion(nameof(ReporteComparativoModelo.IncluirGraficos), false);
        Assert.False(modelo.PuedeDescargar);
    }

    [Fact]
    public async Task CambiarObservaciones_InvalidaPdf()
    {
        var (modelo, _) = await Preparada();
        modelo.GenerarPdf();
        modelo.EstablecerObservaciones("nuevas");
        Assert.False(modelo.PuedeDescargar);
    }

    [Fact]
    public async Task CambiarEvaluacion_InvalidaPdf()
    {
        var (modelo, consulta, _) = Crear();
        consulta.Lista = [Resumen(1), Resumen(2)];
        consulta.Detalles[1] = DetalleComparable(1);
        consulta.Detalles[2] = DetalleComparable(2);
        await modelo.CargarListadoAsync(1);
        modelo.GenerarPdf();
        Assert.True(modelo.PuedeDescargar);
        await modelo.SeleccionarAsync(2);
        Assert.False(modelo.PuedeDescargar);
    }

    [Fact]
    public async Task ErrorEnGeneracion_NoDejaPdfVigente()
    {
        var (modelo, generador) = await Preparada();
        generador.Lanzar = new InvalidOperationException("quest");
        modelo.GenerarPdf();
        Assert.False(modelo.PuedeDescargar);
        Assert.Equal(OperationMessageKind.Error, modelo.MensajeTipo);
    }

    [Fact]
    public async Task EvaluacionNoComparable_PermiteGenerar()
    {
        var (modelo, _) = await Preparada(3, comparable: false);
        Assert.True(modelo.SinComparacion);
        Assert.True(modelo.PuedeGenerar); // informativo
        modelo.GenerarPdf();
        Assert.True(modelo.PuedeDescargar);
    }

    // ------------------------------------------------------------------ navegación

    [Fact]
    public async Task Cancelar_ConSeleccion_VaAComparacion()
    {
        var (modelo, _) = await Preparada(6);
        Assert.Equal("/comparacion/6", modelo.DestinoCancelar());
    }

    [Fact]
    public void Cancelar_SinSeleccion_VaAResultados()
    {
        var (modelo, _, _) = Crear();
        Assert.Equal("/resultados", modelo.DestinoCancelar());
    }

    [Fact]
    public void UrlReporte_UsaConfiguracionId_SinMetricaId()
    {
        var url = ComparacionResultadosModelo.UrlReporte(42);
        Assert.Equal("/reportes?configuracionId=42", url);
        Assert.DoesNotContain("metricaId", url);
    }
}
