using System.Collections.Concurrent;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Execution;

public class StressExecutionRunnerTests
{
    private static readonly Dictionary<string, int> Ids = new()
    {
        [NombresMetrica.Cpu] = 1,
        [NombresMetrica.MemoriaRam] = 2,
        [NombresMetrica.TiempoRespuesta] = 3,
        [NombresMetrica.Latencia] = 4,
        [NombresMetrica.Throughput] = 5,
        [NombresMetrica.TamanoAplicacion] = 6
    };

    private static List<Metrica> Metricas(params string[] nombres) =>
        nombres.Select(n => new Metrica { Id = Ids[n], Nombre = n, Unidad = "u" }).ToList();

    private static readonly string[] Seis =
    [
        NombresMetrica.Cpu, NombresMetrica.MemoriaRam, NombresMetrica.TiempoRespuesta,
        NombresMetrica.Latencia, NombresMetrica.Throughput, NombresMetrica.TamanoAplicacion
    ];

    // --------------------------------------------------------------- fakes

    private sealed class FixedTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() { _now = _now.AddSeconds(1); return _now; }
    }

    private sealed class StressFakeRepo : IEvaluationExecutionRepository
    {
        public List<NivelEstres> Niveles { get; } = [];
        public string? EstadoEjecucionFinal { get; private set; }
        private int _nivelId;

        public Task<ConfiguracionEvaluacion?> GetConfiguracionAsync(int id, CancellationToken ct = default) => Task.FromResult<ConfiguracionEvaluacion?>(null);
        public Task ActualizarEstadoConfiguracionAsync(int id, string estado, CancellationToken ct = default) => Task.CompletedTask;
        public Task AgregarEjecucionAsync(Ejecucion e, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> CrearEjecucionEnProcesoAsync(Ejecucion e, CancellationToken ct = default) => Task.FromResult(1);

        public Task<int> AgregarNivelEnProcesoAsync(NivelEstres nivel, CancellationToken ct = default)
        {
            nivel.Id = ++_nivelId;
            Niveles.Add(nivel);
            return Task.FromResult(nivel.Id);
        }

        public Task CompletarNivelAsync(int nivelId, string estado, DateTime fin, long ok, long fail, string? detalle,
            IReadOnlyList<(int MetricaId, decimal Valor)> resultados, CancellationToken ct = default)
        {
            var nivel = Niveles.First(n => n.Id == nivelId);
            nivel.Estado = estado; nivel.FechaFin = fin; nivel.SolicitudesExitosas = (int)ok; nivel.SolicitudesFallidas = (int)fail; nivel.Detalle = detalle;
            foreach (var (m, v) in resultados) nivel.Resultados.Add(new ResultadoMetricaNivelEstres { NivelEstresId = nivelId, MetricaId = m, Valor = v });
            return Task.CompletedTask;
        }

        public Task FinalizarEjecucionAsync(int ejecucionId, string estado, DateTime fin, IReadOnlyList<(int MetricaId, decimal Valor)>? resumen, CancellationToken ct = default)
        {
            EstadoEjecucionFinal = estado;
            return Task.CompletedTask;
        }

        public decimal? ValorNivel(NivelEstres nivel, string metrica) =>
            nivel.Resultados.FirstOrDefault(r => r.MetricaId == Ids[metrica])?.Valor;
    }

    private sealed class StressFakeApp : ITargetApp
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
            // CPU acelera (aísla por nivel: niveles posteriores → mayor %); RAM crece.
            return new ResourceSample(_n * _n * 10.0, (100L + _n) * 1024 * 1024, _n * 1000.0);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StressLoadRunner : ILoadRunner
    {
        public ConcurrentQueue<LoadPhaseRequest> Requests { get; } = new();
        public int LevelDelayMs { get; init; } = 80;
        public Func<int, LoadPhaseResult> LevelResult { get; init; } = _ => new LoadPhaseResult(true, 100, 0, 10, 20, 50);
        public int CancelOnLevel { get; init; } = -1;
        public Action<int>? OnLevelCompleted { get; init; }
        private int _levelCall;

        public async Task<LoadPhaseResult> RunPhaseAsync(LoadPhaseRequest request, CancellationToken ct = default)
        {
            Requests.Enqueue(request);
            if (request.Usuarios == 1)
            {
                return new LoadPhaseResult(true, 1, 0, 1, 1, 1); // warm-up (resultado descartado)
            }

            var nivel = Interlocked.Increment(ref _levelCall);
            if (nivel == CancelOnLevel)
            {
                await Task.Delay(5000, ct); // permanece hasta que el test cancele
            }
            else
            {
                await Task.Delay(LevelDelayMs, ct);
            }

            OnLevelCompleted?.Invoke(nivel);
            return LevelResult(nivel);
        }
    }

    private static StressExecutionContext Contexto(
        StressFakeApp app, IReadOnlyList<Metrica> metricas, ParametrosEstres parametros, long publishedSize = 10 * 1024 * 1024) =>
        new(EjecucionId: 1, RutaObjetivo: "/", App: app,
            Version: new PreparedVersion(VersionesDotNet.Net8, "net8.0", "/pub", "/pub/app.dll", "8.0.425", publishedSize),
            Paso: new PasoEjecucion(VersionesDotNet.Net8, 1), Orden: 1, TotalPasos: 1,
            MetricasSeleccionadas: metricas, Parametros: parametros, IntervaloMuestreoMs: 10);

    private static StressExecutionRunner Runner(StressFakeRepo repo, ILoadRunner load) =>
        new(repo, load, new FixedTime(), NullLogger<StressExecutionRunner>.Instance);

    private static ParametrosEstres Params(int warmUp = 0) => new(5, 5, 15, 2, warmUp, 200);

    // --------------------------------------------------------------- protocolo

    [Fact]
    public async Task WarmUpUnaVez_SinRampUp_NivelesKeepConstant()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner();
        var ctx = Contexto(new StressFakeApp(), Metricas(Seis), Params(warmUp: 1));

        await Runner(repo, load).EjecutarNivelesAsync(ctx, null, CancellationToken.None);

        var reqs = load.Requests.ToList();
        Assert.Equal(1, reqs.Count(r => r.Usuarios == 1));       // warm-up exactamente una vez
        Assert.DoesNotContain(reqs, r => r.Ramping);             // nunca ramp-up
        var niveles = reqs.Where(r => r.Usuarios != 1).ToList();
        Assert.Equal(new[] { 5, 10, 15 }, niveles.Select(r => r.Usuarios).ToArray());
        Assert.All(niveles, r =>
        {
            Assert.False(r.Ramping);
            Assert.Equal(TimeSpan.FromSeconds(2), r.Duracion);   // DuracionNivelSegundos
            Assert.Equal(200, r.IntervaloSolicitudesMs);
        });
    }

    [Fact]
    public async Task SinWarmUp_NoHaySolicitudDeUnUsuario()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner();
        await Runner(repo, load).EjecutarNivelesAsync(Contexto(new StressFakeApp(), Metricas(Seis), Params(warmUp: 0)), null, CancellationToken.None);
        Assert.DoesNotContain(load.Requests, r => r.Usuarios == 1);
    }

    // --------------------------------------------------------------- métricas por nivel

    [Fact]
    public async Task MetricasPorNivel_UsanP95YRps_YNoSeMezclan()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner
        {
            LevelResult = lvl => new LoadPhaseResult(true, 100 + lvl, 0, 10.0 * lvl, 20.0 * lvl, 50.0 + lvl)
        };
        await Runner(repo, load).EjecutarNivelesAsync(Contexto(new StressFakeApp(), Metricas(Seis), Params()), null, CancellationToken.None);

        Assert.Equal(3, repo.Niveles.Count);
        for (var i = 0; i < 3; i++)
        {
            var lvl = i + 1;
            Assert.Equal(10.0m * lvl, repo.ValorNivel(repo.Niveles[i], NombresMetrica.TiempoRespuesta)); // Mean del nivel
            Assert.Equal(20.0m * lvl, repo.ValorNivel(repo.Niveles[i], NombresMetrica.Latencia));        // P95 del nivel
            Assert.Equal(50.0m + lvl, repo.ValorNivel(repo.Niveles[i], NombresMetrica.Throughput));      // RPS del nivel
        }

        // CPU/RAM independientes por nivel (ventanas separadas → valores distintos).
        Assert.NotEqual(repo.ValorNivel(repo.Niveles[0], NombresMetrica.Cpu), repo.ValorNivel(repo.Niveles[2], NombresMetrica.Cpu));
        Assert.NotEqual(repo.ValorNivel(repo.Niveles[0], NombresMetrica.MemoriaRam), repo.ValorNivel(repo.Niveles[2], NombresMetrica.MemoriaRam));
    }

    [Fact]
    public async Task MetricaNoSeleccionada_NoSePersiste_YTamanoNuncaPorNivel()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner();
        await Runner(repo, load).EjecutarNivelesAsync(
            Contexto(new StressFakeApp(), Metricas(NombresMetrica.Cpu, NombresMetrica.Latencia, NombresMetrica.TamanoAplicacion), Params()),
            null, CancellationToken.None);

        Assert.All(repo.Niveles, n =>
        {
            Assert.Equal(2, n.Resultados.Count); // solo CPU y Latencia (Tamaño nunca por nivel)
            Assert.DoesNotContain(n.Resultados, r => r.MetricaId == Ids[NombresMetrica.TamanoAplicacion]);
            Assert.DoesNotContain(n.Resultados, r => r.MetricaId == Ids[NombresMetrica.Throughput]);
        });
    }

    // --------------------------------------------------------------- resumen final

    [Fact]
    public async Task ResumenFinal_EsElNivelMaximo_ConTamano()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner
        {
            LevelResult = lvl => new LoadPhaseResult(true, 100 + lvl, 0, 10.0 * lvl, 20.0 * lvl, 50.0 + lvl)
        };
        var ctx = Contexto(new StressFakeApp(), Metricas(Seis), Params(), publishedSize: 12 * 1024 * 1024);
        var outcome = await Runner(repo, load).EjecutarNivelesAsync(ctx, null, CancellationToken.None);

        Assert.Equal(EstadosEjecucion.Completada, outcome.EstadoEjecucion);
        Assert.NotNull(outcome.ResumenResultados);
        decimal Res(string m) => outcome.ResumenResultados!.Single(r => r.MetricaId == Ids[m]).Valor;
        Assert.Equal(30.0m, Res(NombresMetrica.TiempoRespuesta)); // nivel 3: 10*3
        Assert.Equal(60.0m, Res(NombresMetrica.Latencia));        // 20*3
        Assert.Equal(53.0m, Res(NombresMetrica.Throughput));      // 50+3
        Assert.Equal(12m, Res(NombresMetrica.TamanoAplicacion));  // PublishedSize (MB)
    }

    // --------------------------------------------------------------- saturación

    [Fact]
    public async Task Saturacion_CeroExitosas_DetieneYPersistaSoloCpuRam()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner
        {
            LevelResult = lvl => lvl < 3
                ? new LoadPhaseResult(true, 100, 0, 10, 20, 50)
                : new LoadPhaseResult(true, 0, 70, 0, 0, 0) // nivel 3 satura
        };
        var outcome = await Runner(repo, load).EjecutarNivelesAsync(Contexto(new StressFakeApp(), Metricas(Seis), Params()), null, CancellationToken.None);

        Assert.Equal(EstadosEjecucion.CompletadaConSaturacion, outcome.EstadoEjecucion);
        Assert.False(outcome.EsFalloTecnico);
        Assert.Null(outcome.ResumenResultados);

        Assert.Equal(3, repo.Niveles.Count); // no se intenta un nivel 4
        Assert.Equal(EstadosNivelEstres.Completado, repo.Niveles[0].Estado);
        Assert.Equal(EstadosNivelEstres.Completado, repo.Niveles[1].Estado);
        var sat = repo.Niveles[2];
        Assert.Equal(EstadosNivelEstres.Saturado, sat.Estado);
        Assert.Equal(0, sat.SolicitudesExitosas);
        Assert.Equal(70, sat.SolicitudesFallidas);
        // CPU/RAM confiables persistidas; Mean/P95/RPS NO (no se inventan como 0).
        Assert.NotNull(repo.ValorNivel(sat, NombresMetrica.Cpu));
        Assert.NotNull(repo.ValorNivel(sat, NombresMetrica.MemoriaRam));
        Assert.Null(repo.ValorNivel(sat, NombresMetrica.TiempoRespuesta));
        Assert.Null(repo.ValorNivel(sat, NombresMetrica.Latencia));
        Assert.Null(repo.ValorNivel(sat, NombresMetrica.Throughput));
    }

    [Fact]
    public async Task ProcesoMuereDuranteNivel_EsSaturacion_SinMetricasParciales()
    {
        var repo = new StressFakeRepo();
        var app = new StressFakeApp();
        var load = new StressLoadRunner
        {
            OnLevelCompleted = lvl => { if (lvl == 2) app.ForceExited = true; }
        };
        var outcome = await Runner(repo, load).EjecutarNivelesAsync(Contexto(app, Metricas(Seis), Params()), null, CancellationToken.None);

        Assert.Equal(EstadosEjecucion.CompletadaConSaturacion, outcome.EstadoEjecucion);
        Assert.False(outcome.EsFalloTecnico);
        Assert.Equal(2, repo.Niveles.Count); // no se intenta el nivel 3
        var muerto = repo.Niveles[1];
        Assert.Equal(EstadosNivelEstres.Saturado, muerto.Estado);
        Assert.Contains("proceso", muerto.Detalle!, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(muerto.Resultados); // ventana no completó → sin métricas parciales
    }

    // --------------------------------------------------------------- fallo técnico

    [Fact]
    public async Task FalloTecnicoDelLoadRunner_MarcaFallidoYDetiene()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner
        {
            LevelResult = lvl => lvl < 2 ? new LoadPhaseResult(true, 100, 0, 10, 20, 50) : new LoadPhaseResult(false, 0, 0, 0, 0, 0)
        };
        var outcome = await Runner(repo, load).EjecutarNivelesAsync(Contexto(new StressFakeApp(), Metricas(Seis), Params()), null, CancellationToken.None);

        Assert.Equal(EstadosEjecucion.Fallida, outcome.EstadoEjecucion);
        Assert.True(outcome.EsFalloTecnico);
        Assert.Equal(2, repo.Niveles.Count);
        Assert.Equal(EstadosNivelEstres.Completado, repo.Niveles[0].Estado);
        Assert.Equal(EstadosNivelEstres.Fallido, repo.Niveles[1].Estado);
    }

    // --------------------------------------------------------------- cancelación

    [Fact]
    public async Task Cancelacion_DuranteNivel_MarcaCanceladoYConservaPrevios()
    {
        var repo = new StressFakeRepo();
        var load = new StressLoadRunner { CancelOnLevel = 2 };
        using var cts = new CancellationTokenSource();
        var tarea = Runner(repo, load).EjecutarNivelesAsync(Contexto(new StressFakeApp(), Metricas(Seis), Params()), null, cts.Token);

        await Task.Delay(200);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tarea);
        Assert.Equal(2, repo.Niveles.Count);
        Assert.Equal(EstadosNivelEstres.Completado, repo.Niveles[0].Estado); // nivel 1 conservado
        Assert.Equal(EstadosNivelEstres.Cancelado, repo.Niveles[1].Estado);
    }
}
