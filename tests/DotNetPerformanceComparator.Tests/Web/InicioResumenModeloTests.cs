using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Pages;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

/// <summary>
/// Pruebas del presentador del "Resumen comparativo reciente" de M-01 (CU-08, C-11.3): selección de la
/// evaluación reciente comparable, estados y small multiples. Usa un <see cref="IConsultaResultadosService"/>
/// falso y el motor real de comparación (puro); no repite la aritmética de CU-07.
/// </summary>
public class InicioResumenModeloTests
{
    private const int Cpu = 1, Ram = 2, Tam = 6;
    private const string Net8 = VersionesDotNet.Net8;
    private const string Net10 = VersionesDotNet.Net10;

    private sealed class FakeConsulta : IConsultaResultadosService
    {
        public IReadOnlyList<EvaluacionResumenConsulta> Lista { get; set; } = [];
        public Dictionary<int, EvaluacionDetalleConsulta?> Detalles { get; } = new();
        public Exception? LanzarEnListado { get; set; }

        public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken ct = default)
        {
            if (LanzarEnListado is not null) throw LanzarEnListado;
            return Task.FromResult(Lista);
        }

        public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Detalles.GetValueOrDefault(id));
    }

    private static InicioResumenModelo Crear(FakeConsulta fake) =>
        new(fake, new ComparacionResultadosService(), NullLogger.Instance);

    private static ResultadoMetricaConsulta R(int id, decimal valor, string nombre = "M", string unidad = "u") => new(id, nombre, unidad, valor);
    private static MetricaSeleccionadaConsulta M(int id, string nombre, string unidad) => new(id, nombre, unidad);

    private static NivelEstresConsulta Niv(int usuarios, string estado, params ResultadoMetricaConsulta[] resultados) =>
        new(usuarios, usuarios, estado, 10, 0, null, resultados);

    private static EjecucionConsulta Eje(string version, int rep, string estado,
        ResultadoMetricaConsulta[]? resultados = null, NivelEstresConsulta[]? niveles = null) =>
        new(version, rep, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], niveles ?? []);

    private static EvaluacionResumenConsulta Resumen(int id, DateTime fecha, bool tiene = true) =>
        new(id, $"Eval {id}", "App", "Carga", "Finalizada", 2, fecha, tiene);

    private static EvaluacionDetalleConsulta Detalle(int id, string tipo, MetricaSeleccionadaConsulta[] metricas, params EjecucionConsulta[] ejecuciones) =>
        new(id, $"Eval {id}", "App", ".NET 8 → .NET 10 (Exitosa)", tipo, "Finalizada", ejecuciones.Length, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), metricas, ejecuciones);

    /// <summary>Detalle de Carga comparable (CPU y RAM en ambas versiones).</summary>
    private static EvaluacionDetalleConsulta DetalleComparable(int id) => Detalle(id, TiposPruebaConocidos.Carga,
        [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")],
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Ram, 80m)]),
        Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Ram, 78m)]));

    /// <summary>Detalle con resultados pero sin par comparable (solo .NET 8).</summary>
    private static EvaluacionDetalleConsulta DetalleNoComparable(int id) => Detalle(id, TiposPruebaConocidos.Carga,
        [M(Cpu, "CPU", "%")],
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)]));

    private static readonly DateTime T1 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

    // ------------------------------------------------------------------ estados

    [Fact]
    public async Task SinEvaluaciones_EstadoVacio()
    {
        var modelo = Crear(new FakeConsulta { Lista = [] });
        await modelo.CargarAsync();
        Assert.False(modelo.HayResumen);
        Assert.Null(modelo.Evaluacion);
        Assert.Empty(modelo.Graficos);
    }

    [Fact]
    public async Task SinResultadosComparables_EstadoVacio()
    {
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = DetalleNoComparable(1);
        var modelo = Crear(fake);
        await modelo.CargarAsync();
        Assert.False(modelo.HayResumen);
    }

    [Fact]
    public async Task Error_EstadoSeguro()
    {
        var modelo = Crear(new FakeConsulta { LanzarEnListado = new InvalidOperationException("db") });
        await modelo.CargarAsync();
        Assert.True(modelo.Error);
        Assert.False(modelo.HayResumen);
    }

    // ------------------------------------------------------------------ selección "reciente comparable"

    [Fact]
    public async Task EvaluacionRecienteComparable_SeSelecciona()
    {
        var fake = new FakeConsulta { Lista = [Resumen(5, T2)] };
        fake.Detalles[5] = DetalleComparable(5);
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.True(modelo.HayResumen);
        Assert.Equal(5, modelo.Evaluacion!.Id);
    }

    [Fact]
    public async Task MasRecienteNoComparable_UsaLaSiguienteComparable()
    {
        // La lista llega ya ordenada (reciente primero). La reciente (id 9) no es comparable; se usa la 3.
        var fake = new FakeConsulta { Lista = [Resumen(9, T2), Resumen(3, T1)] };
        fake.Detalles[9] = DetalleNoComparable(9);
        fake.Detalles[3] = DetalleComparable(3);
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal(3, modelo.Evaluacion!.Id); // respeta el orden y salta la no comparable
    }

    [Fact]
    public async Task DosComparables_EligeLaPrimeraDelListado()
    {
        var fake = new FakeConsulta { Lista = [Resumen(9, T2), Resumen(3, T1)] };
        fake.Detalles[9] = DetalleComparable(9);
        fake.Detalles[3] = DetalleComparable(3);
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal(9, modelo.Evaluacion!.Id); // la más reciente
    }

    [Fact]
    public async Task EvaluacionSinResultados_NoSeInspecciona()
    {
        // id 2 marcada sin resultados → no se carga su detalle (no está en Detalles); id 1 comparable.
        var fake = new FakeConsulta { Lista = [Resumen(2, T2, tiene: false), Resumen(1, T1)] };
        fake.Detalles[1] = DetalleComparable(1);
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal(1, modelo.Evaluacion!.Id);
    }

    // ------------------------------------------------------------------ gráficos (small multiples)

    [Fact]
    public async Task VariasMetricas_GeneraSmallMultiples()
    {
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = DetalleComparable(1);
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal(new[] { "CPU", "Memoria RAM" }, modelo.Graficos.Select(g => g.Category));
    }

    [Fact]
    public async Task MetricaNoComparable_SeOmite_SinCero()
    {
        var detalle = Detalle(1, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(2, "Latencia", "ms")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(2, 1m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m)])); // sin segunda métrica
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = detalle;
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal("CPU", Assert.Single(modelo.Graficos).Category); // la otra no se grafica como 0
    }

    [Fact]
    public async Task ValorCero_Permanece()
    {
        var detalle = Detalle(1, TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0m)]));
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = detalle;
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        var g = Assert.Single(modelo.Graficos);
        Assert.Equal(0m, g.Net8Value);
        Assert.Equal(0m, g.Net10Value);
    }

    [Fact]
    public async Task Tamano_PresenteComoConstante()
    {
        var detalle = Detalle(1, TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 8m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 7m)]));
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = detalle;
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.Equal("Tamaño de la aplicación", Assert.Single(modelo.Graficos).Category);
    }

    // ------------------------------------------------------------------ Estrés (solo resumen en Inicio)

    [Fact]
    public async Task EstresConResumen_SeMuestra()
    {
        var detalle = Detalle(1, TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.6m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.6m))]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m))]));
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = detalle;
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.True(modelo.HayResumen);
        Assert.Equal("CPU", Assert.Single(modelo.Graficos).Category);
    }

    [Fact]
    public async Task EstresSaturadoSinResumen_NoFabricaGrafico()
    {
        var detalle = Detalle(1, TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.6m)), Niv(20, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(20, EstadosNivelEstres.Saturado)]));
        var fake = new FakeConsulta { Lista = [Resumen(1, T1)] };
        fake.Detalles[1] = detalle;
        var modelo = Crear(fake);
        await modelo.CargarAsync();

        Assert.False(modelo.HayResumen); // sin resumen comparable → no se fabrica con el último nivel
        Assert.Empty(modelo.Graficos);
    }
}
