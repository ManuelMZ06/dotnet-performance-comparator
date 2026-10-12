using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetPerformanceComparator.Tests.Execution;

public class EvaluationExecutionSessionManagerTests
{
    /// <summary>Motor falso controlable: bloquea hasta que el test lo complete, observa el token y permite inyectar progreso/excepción.</summary>
    private sealed class FakeEngine : IEjecutarEvaluacionService
    {
        public TaskCompletionSource<string> Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception? Throw { get; init; }
        public Action<IProgress<EvaluationExecutionProgress>>? OnStart { get; init; }
        public volatile bool Entro;

        public Task<ConfiguracionEjecutableInfo?> ObtenerInfoAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<ConfiguracionEjecutableInfo?>(null);

        public async Task<EjecutarEvaluacionResult> EjecutarAsync(
            EjecutarEvaluacionRequest request, IProgress<EvaluationExecutionProgress>? progress = null, CancellationToken ct = default)
        {
            Entro = true;
            if (Throw is not null) throw Throw;
            if (OnStart is not null && progress is not null) OnStart(progress);

            await using var reg = ct.Register(() => Continuar.TrySetResult(EstadosConfiguracionEvaluacion.Cancelada));
            var estado = await Continuar.Task;
            var mensaje = estado == EstadosConfiguracionEvaluacion.Cancelada ? "Evaluación cancelada." : "ok";
            return new EjecutarEvaluacionResult(estado == EstadosConfiguracionEvaluacion.Finalizada, mensaje, estado);
        }
    }

    private static (IEvaluationExecutionSessionManager Manager, FakeEngine Engine) Crear(FakeEngine? engine = null)
    {
        var fake = engine ?? new FakeEngine();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEjecutarEvaluacionService>(fake);
        services.AddSingleton<IEvaluationExecutionSessionManager, EvaluationExecutionSessionManager>();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IEvaluationExecutionSessionManager>(), fake);
    }

    private static async Task<bool> EsperarAsync(Func<bool> condicion, int timeoutMs = 3000)
    {
        var fin = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < fin)
        {
            if (condicion()) return true;
            await Task.Delay(15);
        }
        return condicion();
    }

    [Fact]
    public async Task Start_CreaSesionActiva()
    {
        var (manager, engine) = Crear();
        var r = await manager.StartAsync(1);

        Assert.True(r.Iniciada);
        Assert.True(await EsperarAsync(() => engine.Entro));
        var snap = manager.GetSnapshot(1);
        Assert.NotNull(snap);
        Assert.True(snap!.Activa);

        engine.Continuar.TrySetResult(EstadosConfiguracionEvaluacion.Finalizada);
    }

    [Fact]
    public async Task SegundoStart_EsRechazadoMientrasHayActiva()
    {
        var (manager, engine) = Crear();
        await manager.StartAsync(1);
        Assert.True(await EsperarAsync(() => engine.Entro));

        var r2 = await manager.StartAsync(2);
        Assert.False(r2.Iniciada);
        Assert.Contains("ya existe", r2.Mensaje, StringComparison.OrdinalIgnoreCase);

        engine.Continuar.TrySetResult(EstadosConfiguracionEvaluacion.Finalizada);
    }

    [Fact]
    public async Task TaskFinaliza_DejaDeEstarActiva()
    {
        var (manager, engine) = Crear();
        await manager.StartAsync(1);
        Assert.True(await EsperarAsync(() => engine.Entro));

        engine.Continuar.TrySetResult(EstadosConfiguracionEvaluacion.Finalizada);

        Assert.True(await EsperarAsync(() => manager.GetSnapshot(1) is { Activa: false }));
        Assert.Equal(EstadoSesion.Finalizada, manager.GetSnapshot(1)!.EstadoSesion);
    }

    [Fact]
    public async Task ExcepcionInesperada_SeObservaYMarcaFallida()
    {
        var (manager, _) = Crear(new FakeEngine { Throw = new InvalidOperationException("boom") });
        await manager.StartAsync(1);

        Assert.True(await EsperarAsync(() => manager.GetSnapshot(1) is { EstadoSesion: EstadoSesion.Fallida }));
    }

    [Fact]
    public async Task Reconexion_SegundoObservadorVeElMismoProgreso()
    {
        var engine = new FakeEngine
        {
            OnStart = p => p.Report(new EvaluationExecutionProgress(
                EstadosConfiguracionEvaluacion.EnEjecucion, FaseEjecucion.Medicion, VersionesDotNet.Net8, 1,
                OrdenGlobal: 1, TotalPasos: 5, LogLine: "midiendo...", CpuActual: 0.42m, RamActual: 83m))
        };
        var (manager, _) = Crear(engine);
        await manager.StartAsync(1);

        Assert.True(await EsperarAsync(() => manager.GetSnapshot(1)?.CpuActual is not null));
        var snap = manager.GetSnapshot(1)!; // "segundo observador"
        Assert.Equal(20, snap.ProgresoPorcentaje);
        Assert.Equal(FaseEjecucion.Medicion, snap.Fase);
        Assert.Equal(VersionesDotNet.Net8, snap.VersionActual);
        Assert.Equal(0.42m, snap.CpuActual);
        Assert.Contains("midiendo...", snap.Logs);
        Assert.Single(snap.SerieCpu); // la serie de CPU recibió la muestra

        engine.Continuar.TrySetResult(EstadosConfiguracionEvaluacion.Finalizada);
    }

    [Fact]
    public async Task Cancelacion_UsaTokenDelManager_YEsIdempotente()
    {
        var (manager, engine) = Crear();
        await manager.StartAsync(1);
        Assert.True(await EsperarAsync(() => engine.Entro));

        Assert.True(manager.RequestCancellation(1)); // primera: solicita cancelación
        Assert.True(await EsperarAsync(() => manager.GetSnapshot(1) is { EstadoSesion: EstadoSesion.Cancelada }));

        // Repetir o cancelar tras finalizar no cambia el estado final ni lanza.
        Assert.False(manager.RequestCancellation(1));
        Assert.Equal(EstadoSesion.Cancelada, manager.GetSnapshot(1)!.EstadoSesion);
    }

    [Fact]
    public void CancelarConfiguracionInactiva_DevuelveFalse()
    {
        var (manager, _) = Crear();
        Assert.False(manager.RequestCancellation(99));
    }
}
