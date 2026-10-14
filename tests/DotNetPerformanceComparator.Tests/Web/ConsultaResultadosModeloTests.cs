using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class ConsultaResultadosModeloTests
{
    private sealed class FakeServicio : IConsultaResultadosService
    {
        public IReadOnlyList<EvaluacionResumenConsulta> Lista { get; set; } = [];
        public Func<int, EvaluacionDetalleConsulta?>? DetallePorId { get; set; }
        public Exception? LanzarEnListado { get; set; }
        public Dictionary<int, TaskCompletionSource<EvaluacionDetalleConsulta?>>? Compuertas { get; set; }

        public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken ct = default)
        {
            if (LanzarEnListado is not null) throw LanzarEnListado;
            return Task.FromResult(Lista);
        }

        public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
        {
            if (Compuertas is not null && Compuertas.TryGetValue(id, out var tcs))
            {
                return tcs.Task;
            }
            return Task.FromResult(DetallePorId?.Invoke(id));
        }
    }

    private static ConsultaResultadosModelo Crear(FakeServicio fake) => new(fake, NullLogger.Instance);

    private static EvaluacionResumenConsulta Resumen(int id, string nombre = "Eval", bool tiene = true) =>
        new(id, nombre, "App", "Carga", "Finalizada", 2, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), tiene);

    private static EvaluacionDetalleConsulta Detalle(int id, IReadOnlyList<EjecucionConsulta>? ejecuciones = null) =>
        new(id, $"Eval {id}", "App", ".NET 8 → .NET 10 (Exitosa)", "Carga", "Finalizada", 2, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), [], ejecuciones ?? []);

    private static EjecucionConsulta Ejecucion(
        string version, int numero, string estado,
        IReadOnlyList<ResultadoMetricaConsulta>? resultados = null, IReadOnlyList<NivelEstresConsulta>? niveles = null) =>
        new(version, numero, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], niveles ?? []);

    // ---------------------------------------------------------------- listado

    [Fact]
    public async Task CargarListado_Vacio_MarcaListadoVacio()
    {
        var modelo = Crear(new FakeServicio { Lista = [] });
        await modelo.CargarListadoAsync();

        Assert.Empty(modelo.Evaluaciones);
        Assert.True(modelo.ListadoVacio);
    }

    [Fact]
    public async Task CargarListado_ConEvaluaciones_LasExpone()
    {
        var modelo = Crear(new FakeServicio { Lista = [Resumen(1, "A"), Resumen(2, "B", tiene: false)] });
        await modelo.CargarListadoAsync();

        Assert.Equal(2, modelo.Evaluaciones.Count);
        Assert.False(modelo.ListadoVacio);
        Assert.False(modelo.Evaluaciones.Single(e => e.Id == 2).TieneResultados);
    }

    [Fact]
    public async Task CargarListado_Error_MuestraMensajeYListaVacia()
    {
        var modelo = Crear(new FakeServicio { LanzarEnListado = new InvalidOperationException("db") });
        await modelo.CargarListadoAsync();

        Assert.Empty(modelo.Evaluaciones);
        Assert.Equal(OperationMessageKind.Error, modelo.MensajeTipo);
        Assert.False(string.IsNullOrWhiteSpace(modelo.Mensaje));
    }

    // ---------------------------------------------------------------- detalle

    [Fact]
    public async Task Seleccionar_CargaDetalleCorrecto()
    {
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", [new("CPU", "%", 0.25m)])]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(7);

        Assert.Equal(7, modelo.SeleccionId);
        Assert.NotNull(modelo.Detalle);
        Assert.Equal(7, modelo.Detalle!.Id);
        Assert.False(modelo.EvaluacionNoEncontrada);
    }

    [Fact]
    public async Task Seleccionar_Inexistente_MarcaNoEncontrada()
    {
        var modelo = Crear(new FakeServicio { DetallePorId = _ => null });
        await modelo.SeleccionarAsync(99);

        Assert.True(modelo.EvaluacionNoEncontrada);
        Assert.Null(modelo.Detalle);
    }

    [Fact]
    public async Task Seleccionar_SinResultados_TieneResultadosFalse()
    {
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Fallida")]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(3);

        Assert.False(modelo.Detalle!.TieneResultados);
        Assert.Empty(modelo.Detalle.Ejecuciones.Single().Resultados); // sin ceros ficticios
    }

    [Fact]
    public async Task Seleccionar_EstresSaturado_ConservaNivelesSinResumen()
    {
        var niveles = new[]
        {
            new NivelEstresConsulta(1, 5, "Completado", 100, 0, null, [new("CPU", "%", 0.3m)]),
            new NivelEstresConsulta(2, 10, "Saturado", 0, 80, "0 solicitudes exitosas", [new("CPU", "%", 0.9m)])
        };
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "CompletadaConSaturacion", niveles: niveles)]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(5);

        var eje = modelo.Detalle!.Ejecuciones.Single();
        Assert.Empty(eje.Resultados);          // sin resumen
        Assert.Equal(2, eje.Niveles.Count);    // niveles conservados
        Assert.True(modelo.Detalle.TieneResultados);
    }

    [Fact]
    public async Task Seleccionar_NivelSinMetricas_SeRepresentaComoAusencia()
    {
        var niveles = new[] { new NivelEstresConsulta(1, 5, "Cancelado", 0, 0, null, []) };
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Cancelada", niveles: niveles)]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(4);

        Assert.Empty(modelo.Detalle!.Ejecuciones.Single().Niveles.Single().Resultados);
    }

    [Fact]
    public async Task CambioDeSeleccion_UltimaGana_IgnoraRespuestaTardia()
    {
        var fake = new FakeServicio
        {
            Compuertas = new()
            {
                [1] = new TaskCompletionSource<EvaluacionDetalleConsulta?>(),
                [2] = new TaskCompletionSource<EvaluacionDetalleConsulta?>()
            }
        };
        var modelo = Crear(fake);

        var t1 = modelo.SeleccionarAsync(1); // queda pendiente
        var t2 = modelo.SeleccionarAsync(2); // última selección

        fake.Compuertas![2].SetResult(Detalle(2));
        await t2;
        fake.Compuertas[1].SetResult(Detalle(1)); // llega tarde: debe descartarse
        await t1;

        Assert.Equal(2, modelo.SeleccionId);
        Assert.Equal(2, modelo.Detalle!.Id); // la respuesta tardía de id=1 no sobrescribe
    }

    [Fact]
    public async Task CerrarDetalle_LimpiaEstado()
    {
        var modelo = Crear(new FakeServicio { DetallePorId = id => Detalle(id) });
        await modelo.SeleccionarAsync(1);
        modelo.CerrarDetalle();

        Assert.Null(modelo.SeleccionId);
        Assert.Null(modelo.Detalle);
        Assert.False(modelo.EvaluacionNoEncontrada);
    }

    // ---------------------------------------------------------------- formato

    [Theory]
    [InlineData(0.0647, "0.0647")]
    [InlineData(85.2373, "85.2373")]
    [InlineData(48.07, "48.07")]
    [InlineData(100, "100")]
    public void FormatoValor_HastaCuatroDecimalesSinRelleno(decimal valor, string esperado)
    {
        Assert.Equal(esperado, ConsultaResultadosModelo.FormatoValor(valor));
    }

    [Fact]
    public void ValorConUnidad_UsaUnidadDelDto()
    {
        Assert.Equal("0.25 %", ConsultaResultadosModelo.ValorConUnidad(new ResultadoMetricaConsulta("CPU", "%", 0.25m)));
        Assert.Equal("82.5", ConsultaResultadosModelo.ValorConUnidad(new ResultadoMetricaConsulta("X", "", 82.5m)));
    }
}
