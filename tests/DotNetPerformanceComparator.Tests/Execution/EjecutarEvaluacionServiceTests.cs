using System.Collections.Concurrent;
using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Execution;

public class EjecutarEvaluacionServiceTests
{
    private const int CargaId = 1, EstresId = 2;

    // ---------------------------------------------------------------- fakes

    private sealed class FakeRepo : IEvaluationExecutionRepository
    {
        public ConfiguracionEvaluacion? Config { get; init; }
        public List<Ejecucion> Ejecuciones { get; } = [];
        public List<string> Estados { get; } = [];

        public Task<ConfiguracionEvaluacion?> GetConfiguracionAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Config);

        public Task ActualizarEstadoConfiguracionAsync(int id, string estado, CancellationToken ct = default)
        {
            Estados.Add(estado);
            if (Config is not null) Config.Estado = estado;
            return Task.CompletedTask;
        }

        public Task AgregarEjecucionAsync(Ejecucion ejecucion, CancellationToken ct = default)
        {
            Ejecuciones.Add(ejecucion);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGate(bool libre = true) : IEvaluationExecutionGate
    {
        private int _state = libre ? 0 : 1;
        public int Releases { get; private set; }
        public bool TryAcquire() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;
        public void Release() { Releases++; Interlocked.Exchange(ref _state, 0); }
    }

    private sealed class FakePreparer(bool exito = true) : IExecutionWorkspacePreparer
    {
        public int CleanupLlamadas { get; private set; }

        public Task<WorkspacePreparationResult> PrepareAsync(PrepareWorkspaceRequest req, Action<string>? onLog, CancellationToken ct = default)
        {
            if (!exito)
            {
                return Task.FromResult(new WorkspacePreparationResult(false, "falló preparación", null));
            }

            var ws = new PreparedWorkspace("/tmp/ws",
                new PreparedVersion(VersionesDotNet.Net8, "net8.0", "/tmp/ws/net8/publish", "/tmp/ws/net8/publish/app.dll", "8.0.425"),
                new PreparedVersion(VersionesDotNet.Net10, "net10.0", "/tmp/ws/net10/publish", "/tmp/ws/net10/publish/app.dll", "10.0.400"));
            return Task.FromResult(new WorkspacePreparationResult(true, null, ws));
        }

        public Task CleanupAsync(PreparedWorkspace workspace, Action<string>? onLog)
        {
            CleanupLlamadas++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTargetApp(bool ready, bool exits, bool sampleable) : ITargetApp
    {
        private int _n;
        public bool Disposed { get; private set; }
        public Uri BaseUrl { get; } = new("http://127.0.0.1:5000/");
        public bool HasExited => exits;

        public Task<bool> WaitUntilReadyAsync(string rutaObjetivo, CancellationToken ct = default) => Task.FromResult(ready);

        public ResourceSample? Sample()
        {
            if (!sampleable) return null;
            // CPU avanza 500 ms por cada 1000 ms de reloj → ~50 % con 1 proc.
            _n++;
            return new ResourceSample(_n * 500.0, 128L * 1024 * 1024, _n * 1000.0);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeLauncher(Func<ITargetApp> factory) : ITargetAppLauncher
    {
        public List<ITargetApp> Creados { get; } = [];
        public Task<ITargetApp> StartAsync(PreparedVersion version, CancellationToken ct = default)
        {
            var app = factory();
            Creados.Add(app);
            return Task.FromResult(app);
        }
    }

    private sealed class FakeLoadRunner : ILoadRunner
    {
        public long Exitosas { get; init; } = 100;
        public long Fallidas { get; init; }
        public double MediaMs { get; init; } = 12.5;
        public int StableDelayMs { get; init; } = 2200; // deja que el muestreo tome ≥2 muestras

        public ConcurrentBag<LoadPhaseRequest> Llamadas { get; } = [];

        public async Task<LoadPhaseResult> RunPhaseAsync(LoadPhaseRequest request, CancellationToken ct = default)
        {
            Llamadas.Add(request);
            if (!request.Ramping && request.Usuarios > 1)
            {
                await Task.Delay(StableDelayMs, ct); // fase estable
            }

            return new LoadPhaseResult(true, Exitosas, Fallidas, MediaMs);
        }
    }

    private sealed class FixedTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() { _now = _now.AddSeconds(1); return _now; }
    }

    // ---------------------------------------------------------------- helpers

    private static ConfiguracionEvaluacion Config(
        string estado = "Configurada", int tipoId = CargaId, string tipoNombre = "Carga",
        int numeroEjecuciones = 1, bool conEjecuciones = false, params string[] metricas)
    {
        metricas = metricas.Length == 0 ? [NombresMetrica.Cpu, NombresMetrica.MemoriaRam, NombresMetrica.TiempoRespuesta] : metricas;
        var config = new ConfiguracionEvaluacion
        {
            Id = 1, ProyectoId = 1, MigracionId = 1, TipoPruebaId = tipoId, Nombre = "Carga base",
            NumeroEjecuciones = numeroEjecuciones, RutaObjetivo = "/", Estado = estado,
            Proyecto = new ProyectoDotNet { Id = 1, Nombre = "App", RutaOriginal = @"C:\App\App.csproj" },
            Migracion = new Migracion { Id = 1, RutaMigrada = @"C:\App10", Estado = "Exitosa" },
            TipoPrueba = new TipoPrueba { Id = tipoId, Nombre = tipoNombre }
        };

        foreach (var (clave, valor) in new[]
        {
            (ClavesParametroCarga.UsuariosVirtuales, "10"), (ClavesParametroCarga.DuracionSegundos, "15"),
            (ClavesParametroCarga.WarmUpSegundos, "0"), (ClavesParametroCarga.RampUpSegundos, "0"),
            (ClavesParametroCarga.IntervaloSolicitudesMs, "0")
        })
        {
            config.Parametros.Add(new ParametroEvaluacion { Clave = clave, Valor = valor });
        }

        var id = 10;
        foreach (var nombre in metricas)
        {
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = id, Metrica = new Metrica { Id = id, Nombre = nombre, Unidad = "u" } });
            id++;
        }

        if (conEjecuciones)
        {
            config.Ejecuciones.Add(new Ejecucion { Id = 99, VersionDotNet = VersionesDotNet.Net8, NumeroEjecucion = 1, Estado = "Completada" });
        }

        return config;
    }

    private static EjecutarEvaluacionService Crear(
        FakeRepo repo, IEvaluationExecutionGate? gate = null, IExecutionWorkspacePreparer? preparer = null,
        ITargetAppLauncher? launcher = null, ILoadRunner? loadRunner = null) =>
        new(repo, gate ?? new FakeGate(), preparer ?? new FakePreparer(), launcher ?? new FakeLauncher(() => new FakeTargetApp(true, false, true)),
            loadRunner ?? new FakeLoadRunner(), new FixedTime(), NullLogger<EjecutarEvaluacionService>.Instance, intervaloMuestreoMs: 10);

    private static async Task<EjecutarEvaluacionResult> EjecutarAsync(EjecutarEvaluacionService servicio) =>
        await servicio.EjecutarAsync(new EjecutarEvaluacionRequest(1));

    // ---------------------------------------------------------------- validaciones

    [Fact]
    public async Task ConfiguracionInexistente_EsRechazada()
    {
        var repo = new FakeRepo { Config = null };
        Assert.False((await EjecutarAsync(Crear(repo))).Exito);
    }

    [Fact]
    public async Task EstadoNoConfigurada_EsRechazada()
    {
        var repo = new FakeRepo { Config = Config(estado: "Finalizada") };
        Assert.False((await EjecutarAsync(Crear(repo))).Exito);
        Assert.Empty(repo.Estados);
    }

    [Fact]
    public async Task ConfiguracionConEjecuciones_EsRechazada()
    {
        var repo = new FakeRepo { Config = Config(conEjecuciones: true) };
        Assert.False((await EjecutarAsync(Crear(repo))).Exito);
    }

    [Fact]
    public async Task TipoEstres_EsRechazado()
    {
        var repo = new FakeRepo { Config = Config(tipoId: EstresId, tipoNombre: "Estrés") };
        var r = await EjecutarAsync(Crear(repo));
        Assert.False(r.Exito);
        Assert.Contains("no está habilitado", r.Mensaje);
    }

    [Fact]
    public async Task TipoConcurrencia_EsRechazado()
    {
        var repo = new FakeRepo { Config = Config(tipoId: 3, tipoNombre: "Concurrencia") };
        Assert.False((await EjecutarAsync(Crear(repo))).Exito);
    }

    [Fact]
    public async Task MetricaNoSoportada_EsRechazada()
    {
        var repo = new FakeRepo { Config = Config(metricas: [NombresMetrica.Cpu, NombresMetrica.Latencia]) };
        var r = await EjecutarAsync(Crear(repo));
        Assert.False(r.Exito);
        Assert.Contains("Latencia", r.Mensaje);
    }

    [Fact]
    public async Task GateOcupado_RechazaSegundaEvaluacion()
    {
        var repo = new FakeRepo { Config = Config() };
        var r = await EjecutarAsync(Crear(repo, gate: new FakeGate(libre: false)));
        Assert.False(r.Exito);
        Assert.Contains("ya existe una evaluación", r.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Gate_SeLiberaSiempre()
    {
        var gate = new FakeGate();
        var repo = new FakeRepo { Config = Config() };
        await EjecutarAsync(Crear(repo, gate: gate));
        Assert.Equal(1, gate.Releases);
    }

    // ---------------------------------------------------------------- fallos del motor

    [Fact]
    public async Task FalloPreparacion_ConfiguracionFallida()
    {
        var repo = new FakeRepo { Config = Config() };
        var r = await EjecutarAsync(Crear(repo, preparer: new FakePreparer(exito: false)));
        Assert.False(r.Exito);
        Assert.Equal(EstadosConfiguracionEvaluacion.Fallida, r.EstadoFinal);
        Assert.Contains(EstadosConfiguracionEvaluacion.EnEjecucion, repo.Estados);
        Assert.Contains(EstadosConfiguracionEvaluacion.Fallida, repo.Estados);
        Assert.Empty(repo.Ejecuciones);
    }

    [Fact]
    public async Task FalloReadiness_EjecucionFallidaYSinResultados()
    {
        var repo = new FakeRepo { Config = Config() };
        var launcher = new FakeLauncher(() => new FakeTargetApp(ready: false, exits: false, sampleable: true));
        var r = await EjecutarAsync(Crear(repo, launcher: launcher));

        Assert.False(r.Exito);
        var ejecucion = Assert.Single(repo.Ejecuciones);
        Assert.Equal(EstadosEjecucion.Fallida, ejecucion.Estado);
        Assert.Empty(ejecucion.Resultados);
        Assert.Equal(EstadosConfiguracionEvaluacion.Fallida, repo.Config!.Estado);
    }

    [Fact]
    public async Task ProcesoMuere_EjecucionFallida()
    {
        var repo = new FakeRepo { Config = Config() };
        var launcher = new FakeLauncher(() => new FakeTargetApp(ready: true, exits: true, sampleable: false));
        var r = await EjecutarAsync(Crear(repo, launcher: launcher));

        Assert.False(r.Exito);
        Assert.Equal(EstadosEjecucion.Fallida, Assert.Single(repo.Ejecuciones).Estado);
    }

    [Fact]
    public async Task CeroSolicitudesExitosas_EjecucionFallida()
    {
        var repo = new FakeRepo { Config = Config() };
        var load = new FakeLoadRunner { Exitosas = 0, Fallidas = 50 };
        var r = await EjecutarAsync(Crear(repo, loadRunner: load));

        Assert.False(r.Exito);
        var ejecucion = Assert.Single(repo.Ejecuciones);
        Assert.Equal(EstadosEjecucion.Fallida, ejecucion.Estado);
        Assert.Empty(ejecucion.Resultados);
    }

    // ---------------------------------------------------------------- éxito

    [Fact]
    public async Task EjecucionCorrecta_CreaEjecucionCondicionesYResultados()
    {
        var repo = new FakeRepo { Config = Config(numeroEjecuciones: 1) };
        var r = await EjecutarAsync(Crear(repo));

        Assert.True(r.Exito);
        Assert.Equal(EstadosConfiguracionEvaluacion.Finalizada, r.EstadoFinal);
        Assert.Equal(2, repo.Ejecuciones.Count); // .NET 8 y .NET 10
        Assert.All(repo.Ejecuciones, e =>
        {
            Assert.Equal(EstadosEjecucion.Completada, e.Estado);
            Assert.Equal(3, e.Resultados.Count); // CPU, RAM, Tiempo de respuesta
            Assert.Contains(e.Condiciones, c => c.Clave == ClavesCondicionEjecucion.TargetFramework);
            Assert.Contains(e.Condiciones, c => c.Clave == ClavesCondicionEjecucion.SolicitudesExitosas && c.Valor == "100");
        });
        Assert.Contains(EstadosConfiguracionEvaluacion.Finalizada, repo.Estados);
    }

    [Fact]
    public async Task Condiciones_RegistranEntornoProduction()
    {
        var repo = new FakeRepo { Config = Config() };
        await EjecutarAsync(Crear(repo));

        Assert.All(repo.Ejecuciones, e =>
            Assert.Contains(e.Condiciones, c =>
                c.Clave == ClavesCondicionEjecucion.Environment && c.Valor == "Production"));
    }

    [Fact]
    public async Task ResultadosTienenValoresPlausibles()
    {
        var repo = new FakeRepo { Config = Config() };
        await EjecutarAsync(Crear(repo));

        var ejecucion = repo.Ejecuciones.First();
        foreach (var resultado in ejecucion.Resultados)
        {
            Assert.True(resultado.Valor > 0m);
        }
    }

    [Fact]
    public async Task Workspace_SeLimpiaTrasEjecucion()
    {
        var repo = new FakeRepo { Config = Config() };
        var preparer = new FakePreparer();
        await EjecutarAsync(Crear(repo, preparer: preparer));
        Assert.Equal(1, preparer.CleanupLlamadas);
    }

    [Fact]
    public async Task ObtenerInfo_IndicaSiPuedeEjecutar()
    {
        var repo = new FakeRepo { Config = Config() };
        var info = await Crear(repo).ObtenerInfoAsync(1);
        Assert.NotNull(info);
        Assert.True(info!.PuedeEjecutar);
        Assert.Equal("Carga", info.TipoPrueba);
    }

    [Fact]
    public async Task Cancelacion_MarcaCanceladaYLimpiaRecursos()
    {
        var repo = new FakeRepo { Config = Config() };
        var apps = new List<FakeTargetApp>();
        var launcher = new FakeLauncher(() => { var a = new FakeTargetApp(true, false, true); apps.Add(a); return a; });
        var preparer = new FakePreparer();
        var servicio = Crear(repo, preparer: preparer, launcher: launcher, loadRunner: new FakeLoadRunner { StableDelayMs = 5000 });

        using var cts = new CancellationTokenSource();
        var tarea = servicio.EjecutarAsync(new EjecutarEvaluacionRequest(1), null, cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        var r = await tarea;

        Assert.Equal(EstadosConfiguracionEvaluacion.Cancelada, r.EstadoFinal);
        Assert.All(apps, a => Assert.True(a.Disposed));
        Assert.Equal(1, preparer.CleanupLlamadas);
    }
}
