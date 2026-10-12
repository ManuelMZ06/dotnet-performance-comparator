using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Tests.Execution;

public class ConcurrencyExecutionRunnerTests
{
    private static readonly Dictionary<string, int> Ids = new()
    {
        [NombresMetrica.Cpu] = 1, [NombresMetrica.MemoriaRam] = 2, [NombresMetrica.TiempoRespuesta] = 3,
        [NombresMetrica.Latencia] = 4, [NombresMetrica.Throughput] = 5, [NombresMetrica.TamanoAplicacion] = 6
    };

    private static readonly string[] Seis =
    [
        NombresMetrica.Cpu, NombresMetrica.MemoriaRam, NombresMetrica.TiempoRespuesta,
        NombresMetrica.Latencia, NombresMetrica.Throughput, NombresMetrica.TamanoAplicacion
    ];

    private static List<Metrica> Metricas(params string[] n) =>
        n.Select(x => new Metrica { Id = Ids[x], Nombre = x, Unidad = "u" }).ToList();

    private sealed class ConcFakeApp : ITargetApp
    {
        private int _n;
        public bool ForceExited { get; set; }
        public Uri BaseUrl { get; } = new("http://127.0.0.1:5000/");
        public bool HasExited => ForceExited;
        public Task<bool> WaitUntilReadyAsync(string ruta, CancellationToken ct = default) => Task.FromResult(true);
        public ResourceSample? Sample()
        {
            if (ForceExited) return null;
            _n++;
            return new ResourceSample(_n * 100.0, (80L + _n) * 1024 * 1024, _n * 1000.0);
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeLoad : ILoadRunner
    {
        public Task<LoadPhaseResult> RunPhaseAsync(LoadPhaseRequest request, CancellationToken ct = default) =>
            Task.FromResult(new LoadPhaseResult(true, 1, 0, 1, 1, 1));
    }

    private sealed class FakeConcRunner : IConcurrencyRunner
    {
        public Func<ConcurrencyRunRequest, long, ConcurrencyPhaseResult>? Responder { get; init; }
        public bool ThrowOce { get; init; }

        public Task<ConcurrencyPhaseResult> RunAsync(ConcurrencyRunRequest request, CancellationToken ct = default)
        {
            if (ThrowOce) throw new OperationCanceledException();
            var esperadas = (long)request.UsuariosConcurrentes * request.Rondas;
            var res = Responder?.Invoke(request, esperadas)
                ?? new ConcurrencyPhaseResult(true, esperadas, esperadas, 0, 9.0, 18.0, 30.0, 2.5, 1.4, false);
            return Task.FromResult(res);
        }
    }

    private static ConcurrencyExecutionContext Ctx(ConcFakeApp app, IReadOnlyList<Metrica> metricas, long publishedSize = 8 * 1024 * 1024) =>
        new("/", app,
            new PreparedVersion(VersionesDotNet.Net8, "net8.0", "/pub", "/pub/app.dll", "8.0.425", publishedSize),
            new PasoEjecucion(VersionesDotNet.Net8, 1), 1, 1,
            metricas, new ParametrosConcurrencia(10, 5, 0, 0), IntervaloMuestreoMs: 10);

    private static ConcurrencyExecutionRunner Runner(IConcurrencyRunner conc) => new(new FakeLoad(), conc);

    private static decimal Val(ConcurrencyExecutionOutcome o, string m) => o.Resultados!.Single(r => r.MetricaId == Ids[m]).Valor;

    [Fact]
    public async Task Completada_MapeaMeanP95RpsYTamano()
    {
        var o = await Runner(new FakeConcRunner()).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);

        Assert.Equal(EstadosEjecucion.Completada, o.EstadoEjecucion);
        Assert.Equal(50, o.SolicitudesEsperadas);
        Assert.Equal(9.0m, Val(o, NombresMetrica.TiempoRespuesta)); // Mean
        Assert.Equal(18.0m, Val(o, NombresMetrica.Latencia));       // P95
        Assert.Equal(30.0m, Val(o, NombresMetrica.Throughput));     // RPS
        Assert.Equal(8m, Val(o, NombresMetrica.TamanoAplicacion));  // PublishedSize MB
        Assert.Contains(o.Resultados!, r => r.MetricaId == Ids[NombresMetrica.Cpu]);
        Assert.Contains(o.Resultados!, r => r.MetricaId == Ids[NombresMetrica.MemoriaRam]);
    }

    [Fact]
    public async Task ConteoInconsistente_EsFallida()
    {
        var conc = new FakeConcRunner { Responder = (_, esp) => new ConcurrencyPhaseResult(true, esp, 40, 5, 9, 18, 30, 2, 1, false) };
        var o = await Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Fallida, o.EstadoEjecucion);
        Assert.Null(o.Resultados);
    }

    [Fact]
    public async Task CeroExitosas_EsFallida()
    {
        var conc = new FakeConcRunner { Responder = (_, esp) => new ConcurrencyPhaseResult(true, esp, 0, esp, 0, 0, 0, 2, 1, false) };
        var o = await Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Fallida, o.EstadoEjecucion);
        Assert.Null(o.Resultados);
    }

    [Fact]
    public async Task NoIniciado_EsFallida()
    {
        var conc = new FakeConcRunner { Responder = (_, esp) => new ConcurrencyPhaseResult(false, esp, 0, 0, 0, 0, 0, 0, 0, false) };
        var o = await Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Fallida, o.EstadoEjecucion);
    }

    [Fact]
    public async Task TimeoutSincronizacion_EsFallida()
    {
        var conc = new FakeConcRunner { Responder = (_, esp) => new ConcurrencyPhaseResult(true, esp, esp, 0, 9, 18, 30, 2, 1, TimeoutSincronizacion: true) };
        var o = await Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Fallida, o.EstadoEjecucion);
        Assert.Contains("sincronizar", o.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesoMuere_EsFallida()
    {
        var o = await Runner(new FakeConcRunner()).EjecutarAsync(Ctx(new ConcFakeApp { ForceExited = true }, Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Fallida, o.EstadoEjecucion);
    }

    [Fact]
    public async Task AlgunosFallidos_SigueCompletada()
    {
        var conc = new FakeConcRunner { Responder = (_, esp) => new ConcurrencyPhaseResult(true, esp, esp - 5, 5, 9, 18, 30, 2, 1, false) };
        var o = await Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Completada, o.EstadoEjecucion);
        Assert.Equal(45, o.SolicitudesExitosas);
        Assert.Equal(5, o.SolicitudesFallidas);
    }

    [Fact]
    public async Task PruebaCorta_CpuRamValidasConMuestraInicialYFinal()
    {
        // El runner devuelve de inmediato (sin muestras periódicas); CPU/RAM deben venir de la
        // muestra inicial + final que toma el ConcurrencyExecutionRunner.
        var o = await Runner(new FakeConcRunner()).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(NombresMetrica.Cpu, NombresMetrica.MemoriaRam)), null, CancellationToken.None);
        Assert.Equal(EstadosEjecucion.Completada, o.EstadoEjecucion);
        Assert.Equal(2, o.Resultados!.Count);
        Assert.Contains(o.Resultados!, r => r.MetricaId == Ids[NombresMetrica.Cpu]);
        Assert.Contains(o.Resultados!, r => r.MetricaId == Ids[NombresMetrica.MemoriaRam]);
    }

    [Fact]
    public async Task Cancelacion_Propaga()
    {
        var conc = new FakeConcRunner { ThrowOce = true };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Runner(conc).EjecutarAsync(Ctx(new ConcFakeApp(), Metricas(Seis)), null, CancellationToken.None));
    }
}
