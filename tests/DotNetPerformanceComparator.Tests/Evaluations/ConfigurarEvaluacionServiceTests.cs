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
        int? tipoPruebaId = Carga,
        int? ejecuciones = 3,
        string? ruta = "/api/productos",
        IReadOnlyList<int>? metricas = null,
        string? observaciones = null,
        ParametrosCargaInput? carga = null,
        ParametrosEstresInput? estres = null,
        ParametrosConcurrenciaInput? concurrencia = null,
        string? nombre = "Evaluación 01",
        int? proyectoId = 1,
        int? migracionId = 10) =>
        new(nombre, proyectoId, migracionId, tipoPruebaId, ejecuciones, ruta, observaciones,
            metricas ?? [1, 3], carga, estres, concurrencia);

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
        Assert.False((await servicio.GuardarAsync(Req(nombre: nombre, carga: CargaValida()))).Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task ProyectoInexistente_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(proyectoId: 99, carga: CargaValida()))).Exito);

    [Fact]
    public async Task MigracionDeOtroProyecto_EsRechazada()
    {
        var resultado = await Crear().Servicio.GuardarAsync(Req(migracionId: 20, carga: CargaValida()));
        Assert.False(resultado.Exito);
        Assert.Contains("no pertenece", resultado.Mensaje);
    }

    [Fact]
    public async Task MigracionFallida_EsRechazada() =>
        Assert.Contains("exitosa", (await Crear().Servicio.GuardarAsync(Req(migracionId: 30, carga: CargaValida()))).Mensaje);

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
        Assert.Equal(esperado, (await servicio.GuardarAsync(Req(ejecuciones: ejecuciones, carga: CargaValida()))).Exito);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/api/productos?id=1", true)]
    [InlineData("", false)]
    [InlineData("api/productos", false)]
    [InlineData("http://google.com/", false)]
    [InlineData("https://x.com/", false)]
    public async Task RutaObjetivo_SoloAceptaRutasRelativas(string? ruta, bool esperado)
    {
        var (servicio, _) = Crear();
        Assert.Equal(esperado, (await servicio.GuardarAsync(Req(ruta: ruta, carga: CargaValida()))).Exito);
    }

    [Fact]
    public async Task SinMetricas_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(metricas: [], carga: CargaValida()))).Exito);

    [Fact]
    public async Task MetricaInexistente_EsRechazada() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(metricas: [1, 77], carga: CargaValida()))).Exito);

    // ---------------------------------------------------------------- tipo único

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(99)]
    public async Task TipoPruebaInvalido_EsRechazado(int? tipoId)
    {
        var (servicio, repo) = Crear();
        Assert.False((await servicio.GuardarAsync(Req(tipoPruebaId: tipoId, carga: CargaValida()))).Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task Carga_Valida_GuardaConUnicoTipoYSusCincoParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: CargaValida()))).Exito);
        Assert.Equal(Carga, repo.Guardada!.TipoPruebaId);
        Assert.Equal(new[] { "DuracionSegundos", "IntervaloSolicitudesMs", "RampUpSegundos", "UsuariosVirtuales", "WarmUpSegundos" },
            repo.Guardada.Parametros.Select(p => p.Clave).Order());
    }

    [Fact]
    public async Task Estres_Valido_GuardaSusSeisParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipoPruebaId: Estres, estres: EstresValido()))).Exito);
        Assert.Equal(Estres, repo.Guardada!.TipoPruebaId);
        Assert.Equal(6, repo.Guardada.Parametros.Count);
    }

    [Fact]
    public async Task Concurrencia_Valida_GuardaSusCuatroParametros()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Req(tipoPruebaId: Concurrencia, concurrencia: ConcurrenciaValida()))).Exito);
        Assert.Equal(Concurrencia, repo.Guardada!.TipoPruebaId);
        Assert.Equal(4, repo.Guardada.Parametros.Count);
    }

    // ---------------------------------------------------------------- bloque correcto por tipo

    [Fact]
    public async Task Carga_ConParametrosCarga_EsValida() =>
        Assert.True((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: CargaValida()))).Exito);

    [Fact]
    public async Task Carga_ConParametrosEstres_EsRechazada()
    {
        var r = await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: CargaValida(), estres: EstresValido()));
        Assert.False(r.Exito);
        Assert.Contains("no corresponden al tipo", r.Mensaje);
    }

    [Fact]
    public async Task Carga_ConParametrosConcurrencia_EsRechazada() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: CargaValida(), concurrencia: ConcurrenciaValida()))).Exito);

    [Fact]
    public async Task Estres_ConParametrosCarga_EsRechazada() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Estres, estres: EstresValido(), carga: CargaValida()))).Exito);

    [Fact]
    public async Task Estres_ConParametrosEstres_EsValida() =>
        Assert.True((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Estres, estres: EstresValido()))).Exito);

    [Fact]
    public async Task Concurrencia_ConParametrosConcurrencia_EsValida() =>
        Assert.True((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Concurrencia, concurrencia: ConcurrenciaValida()))).Exito);

    [Fact]
    public async Task VariosBloquesDeParametros_EsRechazado()
    {
        var r = await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: CargaValida(), estres: EstresValido(), concurrencia: ConcurrenciaValida()));
        Assert.False(r.Exito);
    }

    // ---------------------------------------------------------------- parámetros inválidos por tipo

    [Theory]
    [InlineData("0", "120", "1", "10", "1")]   // usuarios <= 0
    [InlineData("50", "120", "-1", "10", "1")] // warm-up negativo
    [InlineData(null, "120", "1", "10", "1")]  // falta obligatorio
    public async Task Carga_ParametrosInvalidos_SonRechazados(string? uv, string? dur, string? warm, string? ramp, string? intervalo)
    {
        var (servicio, repo) = Crear();
        Assert.False((await servicio.GuardarAsync(Req(tipoPruebaId: Carga, carga: new ParametrosCargaInput(uv, dur, warm, ramp, intervalo)))).Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task Estres_MaximoMenorQueInicial_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Estres, estres: new ParametrosEstresInput("50", "10", "50", "30", "0", "0")))).Exito);

    [Fact]
    public async Task Concurrencia_RondasCero_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Concurrencia, concurrencia: new ParametrosConcurrenciaInput("20", "0", "0", "100")))).Exito);

    [Fact]
    public async Task TipoSinSusParametros_EsRechazado() =>
        Assert.False((await Crear().Servicio.GuardarAsync(Req(tipoPruebaId: Estres, estres: null))).Exito);

    // ---------------------------------------------------------------- datos generales

    [Fact]
    public async Task ConfiguracionValida_PersisteDatosGenerales()
    {
        var (servicio, repo) = Crear();
        var r = await servicio.GuardarAsync(Req(
            nombre: "  Mi evaluación  ", tipoPruebaId: Carga, ejecuciones: 2, ruta: "/peliculas",
            observaciones: "  nota  ", metricas: [3, 5], carga: CargaValida()));

        Assert.True(r.Exito);
        var c = repo.Guardada!;
        Assert.Equal("Mi evaluación", c.Nombre);
        Assert.Equal(Carga, c.TipoPruebaId);
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
        await servicio.GuardarAsync(Req(tipoPruebaId: Carga, observaciones: "una nota", carga: CargaValida()));
        Assert.DoesNotContain(repo.Guardada!.Parametros, p => p.Valor == "una nota");
    }
}
