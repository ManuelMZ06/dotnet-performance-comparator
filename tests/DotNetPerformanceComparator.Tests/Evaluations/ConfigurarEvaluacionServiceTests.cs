using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Evaluations;

public class ConfigurarEvaluacionServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    // Ids de catálogo: 1 Carga, 2 Estrés, 3 Concurrencia.
    private const int Carga = 1, Estres = 2, Concurrencia = 3;

    private sealed class FakeCatalogo : IEvaluationCatalogRepository
    {
        public List<Migracion> Migraciones { get; } =
        [
            new() { Id = 10, ProyectoId = 1, Estado = EstadosMigracion.Exitosa, FechaInicio = Ahora.UtcDateTime, FechaFin = Ahora.UtcDateTime },
            new() { Id = 20, ProyectoId = 2, Estado = EstadosMigracion.Exitosa, FechaInicio = Ahora.UtcDateTime },
            new() { Id = 30, ProyectoId = 1, Estado = EstadosMigracion.Fallida, FechaInicio = Ahora.UtcDateTime }
        ];

        public Task<IReadOnlyList<ProyectoDotNet>> GetProyectosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>([new ProyectoDotNet { Id = 1, Nombre = "App" }]);

        public Task<IReadOnlyList<Migracion>> GetMigracionesExitosasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Migracion>>(Migraciones.Where(m => m.Estado == EstadosMigracion.Exitosa).ToList());

        public Task<IReadOnlyList<TipoPrueba>> GetTiposPruebaAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TipoPrueba>>(
            [
                new() { Id = Carga, Nombre = TiposPruebaConocidos.Carga },
                new() { Id = Estres, Nombre = TiposPruebaConocidos.Estres },
                new() { Id = Concurrencia, Nombre = TiposPruebaConocidos.Concurrencia }
            ]);

        public Task<IReadOnlyList<Metrica>> GetMetricasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Metrica>>(Enumerable.Range(1, 6).Select(i => new Metrica { Id = i, Nombre = $"M{i}", Unidad = "u" }).ToList());

        public Task<Migracion?> GetMigracionByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Migraciones.FirstOrDefault(m => m.Id == id));
    }

    private sealed class FakeProyectos : IProyectoDotNetRepository
    {
        public Task AddAsync(ProyectoDotNet p, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ProyectoDotNet>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>([new ProyectoDotNet { Id = 1, Nombre = "App" }]);
        public Task<ProyectoDotNet?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(id == 1 ? new ProyectoDotNet { Id = 1, Nombre = "App" } : null);
    }

    private sealed class FakeConfigRepo : IConfiguracionEvaluacionRepository
    {
        public ConfiguracionEvaluacion? Guardada { get; private set; }

        public Task AddAsync(ConfiguracionEvaluacion c, CancellationToken ct = default)
        {
            c.Id = 77;
            Guardada = c;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static (ConfigurarEvaluacionService Servicio, FakeConfigRepo Repo) Crear()
    {
        var repo = new FakeConfigRepo();
        return (new ConfigurarEvaluacionService(new FakeCatalogo(), new FakeProyectos(), repo, new FixedTime(Ahora),
            NullLogger<ConfigurarEvaluacionService>.Instance), repo);
    }

    private static ParametrosCargaInput CargaValida() => new("50", "120", "1", "10", "1");
    private static ParametrosEstresInput EstresValido() => new("10", "10", "50", "30", "0", "0");
    private static ParametrosConcurrenciaInput ConcurrenciaValida() => new("20", "5", "0", "100");

    private static ConfigurarEvaluacionRequest Req(
        int? ejecuciones = 3,
        string? ruta = "/api/productos",
        IReadOnlyList<int>? tipos = null,
        IReadOnlyList<int>? metricas = null,
        string? observaciones = null,
        ParametrosCargaInput? carga = null,
        ParametrosEstresInput? estres = null,
        ParametrosConcurrenciaInput? concurrencia = null,
        string? nombre = "Evaluación 01",
        int? proyectoId = 1,
        int? migracionId = 10) =>
        new(nombre, proyectoId, migracionId, ejecuciones, ruta, observaciones,
            tipos ?? [Carga], metricas ?? [1, 3], carga ?? CargaValida(), estres, concurrencia);

    private static ConfiguracionPrueba PruebaDe(FakeConfigRepo repo, int tipoId) =>
        repo.Guardada!.ConfiguracionesPrueba.Single(cp => cp.TipoPruebaId == tipoId);

    // ---------------------------------------------------------------- generales

    [Fact]
    public async Task ObtenerDatos_MapeaCatalogosYMigracionesExitosas()
    {
        var (servicio, _) = Crear();
        var datos = await servicio.ObtenerDatosAsync();
        Assert.Equal(new[] { 10, 20 }, datos.MigracionesExitosas.Select(m => m.Id));
        Assert.Equal(3, datos.TiposPrueba.Count);
        Assert.Equal(6, datos.Metricas.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task NombreVacio_EsRechazado(string? nombre)
    {
        var (servicio, repo) = Crear();
        Assert.False((await servicio.GuardarAsync(Req(nombre: nombre))).Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task ProyectoInexistente_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(proyectoId: 99))).Exito);

    [Fact]
    public async Task MigracionInexistente_EsRechazada() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(migracionId: 999))).Exito);

    [Fact]
    public async Task MigracionDeOtroProyecto_EsRechazada()
    {
        var resultado = await Crear().Servicio.GuardarAsync(Req(proyectoId: 1, migracionId: 20));
        Assert.False(resultado.Exito);
        Assert.Contains("no pertenece", resultado.Mensaje);
    }

    [Fact]
    public async Task MigracionFallida_EsRechazada() =>
        Assert.Contains("exitosa", (await Crear().Servicio.GuardarAsync(Req(migracionId: 30))).Mensaje);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(null, false)]
    public async Task NumeroEjecuciones_SoloAcepta1a3(int? ejecuciones, bool esperado)
    {
        var (servicio, _) = Crear();
        Assert.Equal(esperado, (await servicio.GuardarAsync(Req(ejecuciones: ejecuciones))).Exito);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/api/productos", true)]
    [InlineData("/api/productos?id=1", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("api/productos", false)]
    [InlineData("http://google.com/", false)]
    [InlineData("https://x.com/", false)]
    public async Task RutaObjetivo_SoloAceptaRutasRelativas(string? ruta, bool esperado)
    {
        var (servicio, _) = Crear();
        Assert.Equal(esperado, (await servicio.GuardarAsync(Req(ruta: ruta))).Exito);
    }

    [Fact]
    public async Task SinTiposDePrueba_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipos: []))).Exito);

    [Fact]
    public async Task SinMetricas_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(metricas: []))).Exito);

    [Fact]
    public async Task TipoInexistente_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipos: [Carga, 99], carga: CargaValida()))).Exito);

    [Fact]
    public async Task MetricaInexistente_EsRechazada() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(metricas: [1, 77]))).Exito);

    // ---------------------------------------------------------------- Carga

    [Theory]
    [InlineData("0", "120", "1", "10", "1")]   // usuarios <= 0
    [InlineData("50", "0", "1", "10", "1")]    // duración <= 0
    [InlineData("50", "120", "-1", "10", "1")] // warm-up negativo
    [InlineData("50", "120", "1", "-1", "1")]  // ramp-up negativo
    [InlineData("50", "120", "1", "10", "-1")] // intervalo negativo
    [InlineData(null, "120", "1", "10", "1")]  // falta un obligatorio
    public async Task Carga_ParametrosInvalidos_SonRechazados(string? uv, string? dur, string? warm, string? ramp, string? intervalo)
    {
        var (servicio, repo) = Crear();
        var r = await servicio.GuardarAsync(Req(carga: new ParametrosCargaInput(uv, dur, warm, ramp, intervalo)));
        Assert.False(r.Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task Carga_Valida_PersisteSusCincoParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipos: [Carga], carga: CargaValida()))).Exito);
        var claves = PruebaDe(repo, Carga).Parametros.Select(p => p.Clave).Order();
        Assert.Equal(new[] { "DuracionSegundos", "IntervaloSolicitudesMs", "RampUpSegundos", "UsuariosVirtuales", "WarmUpSegundos" }, claves);
    }

    // ---------------------------------------------------------------- Estrés

    [Theory]
    [InlineData("0", "10", "50", "30", "0", "0")]   // inicial <= 0
    [InlineData("10", "0", "50", "30", "0", "0")]   // incremento <= 0
    [InlineData("50", "10", "50", "30", "0", "0")]  // máximo <= inicial
    [InlineData("10", "10", "50", "0", "0", "0")]   // duración nivel <= 0
    [InlineData("10", "10", "50", "30", "-1", "0")] // warm-up negativo
    [InlineData("10", "10", "50", "30", "0", "-1")] // intervalo negativo
    public async Task Estres_ParametrosInvalidos_SonRechazados(string ini, string inc, string max, string dur, string warm, string intervalo)
    {
        var (servicio, _) = Crear();
        var r = await servicio.GuardarAsync(Req(tipos: [Estres], estres: new ParametrosEstresInput(ini, inc, max, dur, warm, intervalo)));
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Estres_Valido_PersisteSusSeisParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipos: [Estres], estres: EstresValido()))).Exito);
        Assert.Equal(6, PruebaDe(repo, Estres).Parametros.Count);
    }

    // ---------------------------------------------------------------- Concurrencia

    [Theory]
    [InlineData("0", "5", "0", "100")]   // usuarios <= 0
    [InlineData("20", "0", "0", "100")]  // rondas <= 0
    [InlineData("20", "5", "-1", "100")] // warm-up negativo
    [InlineData("20", "5", "0", "-1")]   // intervalo negativo
    public async Task Concurrencia_ParametrosInvalidos_SonRechazados(string uc, string rondas, string warm, string intervalo)
    {
        var (servicio, _) = Crear();
        var r = await servicio.GuardarAsync(Req(tipos: [Concurrencia], concurrencia: new ParametrosConcurrenciaInput(uc, rondas, warm, intervalo)));
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Concurrencia_Valida_PersisteSusCuatroParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipos: [Concurrencia], concurrencia: ConcurrenciaValida()))).Exito);
        Assert.Equal(4, PruebaDe(repo, Concurrencia).Parametros.Count);
    }

    // ---------------------------------------------------------------- combinaciones

    [Fact]
    public async Task TresTipos_CadaParametroQuedaAsociadoAlTipoCorrecto()
    {
        var (servicio, repo) = Crear();
        var r = await servicio.GuardarAsync(Req(
            tipos: [Carga, Estres, Concurrencia],
            carga: CargaValida(), estres: EstresValido(), concurrencia: ConcurrenciaValida()));

        Assert.True(r.Exito);
        Assert.All(PruebaDe(repo, Carga).Parametros, p => Assert.Equal(Carga, p.TipoPruebaId));
        Assert.All(PruebaDe(repo, Estres).Parametros, p => Assert.Equal(Estres, p.TipoPruebaId));
        Assert.All(PruebaDe(repo, Concurrencia).Parametros, p => Assert.Equal(Concurrencia, p.TipoPruebaId));
        Assert.Contains(PruebaDe(repo, Carga).Parametros, p => p.Clave == ClavesParametroCarga.UsuariosVirtuales);
        Assert.Contains(PruebaDe(repo, Estres).Parametros, p => p.Clave == ClavesParametroEstres.UsuariosIniciales);
        Assert.Contains(PruebaDe(repo, Concurrencia).Parametros, p => p.Clave == ClavesParametroConcurrencia.Rondas);
    }

    [Theory]
    [InlineData(Carga, Estres)]
    [InlineData(Carga, Concurrencia)]
    [InlineData(Estres, Concurrencia)]
    public async Task DosTipos_SeGuardanAmbosConSusParametros(int a, int b)
    {
        var (servicio, repo) = Crear();
        var r = await servicio.GuardarAsync(Req(
            tipos: [a, b], carga: CargaValida(), estres: EstresValido(), concurrencia: ConcurrenciaValida()));

        Assert.True(r.Exito);
        Assert.Equal(2, repo.Guardada!.ConfiguracionesPrueba.Count);
        Assert.NotEmpty(PruebaDe(repo, a).Parametros);
        Assert.NotEmpty(PruebaDe(repo, b).Parametros);
    }

    [Fact]
    public async Task TipoSeleccionadoSinParametros_EsRechazado()
    {
        var (servicio, _) = Crear();
        var r = await servicio.GuardarAsync(Req(tipos: [Estres], estres: null));
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task ParametrosDeTipoNoSeleccionado_NoSePersisten()
    {
        var (servicio, repo) = Crear();
        // Solo Carga seleccionada, pero se envían también parámetros de Estrés.
        var r = await servicio.GuardarAsync(Req(tipos: [Carga], carga: CargaValida(), estres: EstresValido()));

        Assert.True(r.Exito);
        Assert.Equal([Carga], repo.Guardada!.ConfiguracionesPrueba.Select(cp => cp.TipoPruebaId));
    }

    [Fact]
    public async Task ConfiguracionValida_PersisteDatosGenerales()
    {
        var (servicio, repo) = Crear();
        var r = await servicio.GuardarAsync(Req(
            nombre: "  Mi evaluación  ", ejecuciones: 2, ruta: "/peliculas",
            observaciones: "  nota  ", metricas: [3, 5]));

        Assert.True(r.Exito);
        var c = repo.Guardada!;
        Assert.Equal("Mi evaluación", c.Nombre);
        Assert.Equal(2, c.NumeroEjecuciones);
        Assert.Equal("/peliculas", c.RutaObjetivo);
        Assert.Equal("nota", c.Observaciones);
        Assert.Equal(EstadosConfiguracionEvaluacion.Configurada, c.Estado);
        Assert.Equal(Ahora.UtcDateTime, c.FechaCreacion);
        Assert.Equal(new[] { 3, 5 }, c.ConfiguracionesMetrica.Select(m => m.MetricaId));
    }

    [Fact]
    public async Task Observaciones_NoSeGuardaComoParametro()
    {
        var (servicio, repo) = Crear();
        await servicio.GuardarAsync(Req(observaciones: "una nota"));
        Assert.DoesNotContain(PruebaDe(repo, Carga).Parametros, p => p.Valor == "una nota");
    }
}
