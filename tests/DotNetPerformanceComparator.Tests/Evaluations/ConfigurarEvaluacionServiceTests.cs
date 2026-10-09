using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Evaluations;

public class ConfigurarEvaluacionServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeCatalogo : IEvaluationCatalogRepository
    {
        public List<Migracion> Migraciones { get; } =
        [
            new() { Id = 10, ProyectoId = 1, Estado = EstadosMigracion.Exitosa, VersionOrigen = ".NET 8", VersionDestino = ".NET 10", FechaInicio = Ahora.UtcDateTime, FechaFin = Ahora.UtcDateTime },
            new() { Id = 20, ProyectoId = 2, Estado = EstadosMigracion.Exitosa, VersionOrigen = ".NET 8", VersionDestino = ".NET 10", FechaInicio = Ahora.UtcDateTime, FechaFin = Ahora.UtcDateTime },
            new() { Id = 30, ProyectoId = 1, Estado = EstadosMigracion.Fallida, VersionOrigen = ".NET 8", VersionDestino = ".NET 10", FechaInicio = Ahora.UtcDateTime }
        ];

        public Task<IReadOnlyList<ProyectoDotNet>> GetProyectosAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>([new ProyectoDotNet { Id = 1, Nombre = "App" }]);

        public Task<IReadOnlyList<Migracion>> GetMigracionesExitosasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Migracion>>(Migraciones.Where(m => m.Estado == EstadosMigracion.Exitosa).ToList());

        public Task<IReadOnlyList<TipoPrueba>> GetTiposPruebaAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TipoPrueba>>([new() { Id = 1, Nombre = "Carga" }, new() { Id = 2, Nombre = "Estrés" }, new() { Id = 3, Nombre = "Concurrencia" }]);

        public Task<IReadOnlyList<Metrica>> GetMetricasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Metrica>>(Enumerable.Range(1, 6).Select(i => new Metrica { Id = i, Nombre = $"M{i}", Unidad = "u" }).ToList());

        public Task<Migracion?> GetMigracionByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Migraciones.FirstOrDefault(m => m.Id == id));
    }

    private sealed class FakeProyectos : IProyectoDotNetRepository
    {
        public Task AddAsync(ProyectoDotNet proyecto, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ProyectoDotNet>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>([new ProyectoDotNet { Id = 1, Nombre = "App" }]);
        public Task<ProyectoDotNet?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(id == 1 ? new ProyectoDotNet { Id = 1, Nombre = "App" } : null);
    }

    private sealed class FakeConfigRepo : IConfiguracionEvaluacionRepository
    {
        public ConfiguracionEvaluacion? Guardada { get; private set; }
        public Exception? Error { get; init; }

        public Task AddAsync(ConfiguracionEvaluacion configuracion, CancellationToken ct = default)
        {
            if (Error is not null)
            {
                throw Error;
            }

            configuracion.Id = 77;
            Guardada = configuracion;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static (ConfigurarEvaluacionService Servicio, FakeConfigRepo Repo) Crear(FakeConfigRepo? repo = null)
    {
        repo ??= new FakeConfigRepo();
        var servicio = new ConfigurarEvaluacionService(
            new FakeCatalogo(), new FakeProyectos(), repo, new FixedTime(Ahora),
            NullLogger<ConfigurarEvaluacionService>.Instance);
        return (servicio, repo);
    }

    private static ConfigurarEvaluacionRequest Valido(
        string? nombre = "Evaluación 01",
        int? proyectoId = 1,
        int? migracionId = 10,
        int? ejecuciones = 3,
        IReadOnlyList<int>? tipos = null,
        IReadOnlyList<int>? metricas = null,
        ParametrosEvaluacionInput? parametros = null) =>
        new(nombre, proyectoId, migracionId, ejecuciones, tipos ?? [1, 2], metricas ?? [1, 3],
            parametros ?? new ParametrosEvaluacionInput());

    [Fact]
    public async Task ObtenerDatos_MapeaCatalogosYMigracionesExitosas()
    {
        var (servicio, _) = Crear();

        var datos = await servicio.ObtenerDatosAsync();

        Assert.Single(datos.Proyectos);
        Assert.Equal(new[] { 10, 20 }, datos.MigracionesExitosas.Select(m => m.Id));
        Assert.Equal(3, datos.TiposPrueba.Count);
        Assert.Equal(6, datos.Metricas.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreVacio_EsRechazado(string? nombre)
    {
        var (servicio, repo) = Crear();
        var resultado = await servicio.GuardarAsync(Valido(nombre: nombre));
        Assert.False(resultado.Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task ProyectoInexistente_EsRechazado()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(proyectoId: 99))).Exito);
    }

    [Fact]
    public async Task MigracionInexistente_EsRechazada()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(migracionId: 999))).Exito);
    }

    [Fact]
    public async Task MigracionDeOtroProyecto_EsRechazada()
    {
        var (servicio, repo) = Crear();
        var resultado = await servicio.GuardarAsync(Valido(proyectoId: 1, migracionId: 20));
        Assert.False(resultado.Exito);
        Assert.Contains("no pertenece", resultado.Mensaje);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task MigracionFallida_EsRechazada()
    {
        var (servicio, _) = Crear();
        var resultado = await servicio.GuardarAsync(Valido(migracionId: 30));
        Assert.False(resultado.Exito);
        Assert.Contains("exitosa", resultado.Mensaje);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(null)]
    public async Task NumeroEjecucionesInvalido_EsRechazado(int? ejecuciones)
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(ejecuciones: ejecuciones))).Exito);
    }

    [Fact]
    public async Task NumeroEjecucionesAlto_EsAceptado()
    {
        var (servicio, repo) = Crear();
        Assert.True((await servicio.GuardarAsync(Valido(ejecuciones: 100))).Exito);
        Assert.Equal(100, repo.Guardada!.NumeroEjecuciones);
    }

    [Fact]
    public async Task SinTiposDePrueba_EsRechazado()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(tipos: []))).Exito);
    }

    [Fact]
    public async Task SinMetricas_EsRechazado()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(metricas: []))).Exito);
    }

    [Fact]
    public async Task TipoPruebaInexistente_EsRechazado()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(tipos: [1, 99]))).Exito);
    }

    [Fact]
    public async Task MetricaInexistente_EsRechazada()
    {
        var (servicio, _) = Crear();
        Assert.False((await servicio.GuardarAsync(Valido(metricas: [1, 77]))).Exito);
    }

    [Theory]
    [InlineData("abc", null, null)]
    [InlineData(null, "0", null)]      // usuarios virtuales > 0
    [InlineData("-1", null, null)]     // warm-up >= 0
    [InlineData(null, null, "0")]      // duración > 0
    public async Task ParametrosInvalidos_SonRechazados(string? warmUp, string? usuarios, string? duracion)
    {
        var (servicio, repo) = Crear();
        var parametros = new ParametrosEvaluacionInput(WarmUp: warmUp, UsuariosVirtuales: usuarios, DuracionSegundos: duracion);

        var resultado = await servicio.GuardarAsync(Valido(parametros: parametros));

        Assert.False(resultado.Exito);
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task ConfiguracionValida_SeGuardaComoAgregado()
    {
        var (servicio, repo) = Crear();

        var resultado = await servicio.GuardarAsync(Valido(nombre: "  Mi evaluación  ", tipos: [1, 2], metricas: [3, 5]));

        Assert.True(resultado.Exito);
        Assert.Equal(77, resultado.ConfiguracionId);
        var c = repo.Guardada!;
        Assert.Equal("Mi evaluación", c.Nombre);
        Assert.Equal(1, c.ProyectoId);
        Assert.Equal(10, c.MigracionId);
        Assert.Equal(EstadosConfiguracionEvaluacion.Configurada, c.Estado);
        Assert.Equal(Ahora.UtcDateTime, c.FechaCreacion);
        Assert.Equal(DateTimeKind.Utc, c.FechaCreacion.Kind);
        Assert.Equal(new[] { 1, 2 }, c.ConfiguracionesPrueba.Select(p => p.TipoPruebaId));
        Assert.Equal(new[] { 3, 5 }, c.ConfiguracionesMetrica.Select(m => m.MetricaId));
    }

    [Fact]
    public async Task Parametros_SoloCreanFilasParaLosQueTienenValor()
    {
        var (servicio, repo) = Crear();
        var parametros = new ParametrosEvaluacionInput(
            WarmUp: "1", UsuariosVirtuales: "50", DuracionSegundos: "120",
            RampUpSegundos: null, Intervalo: "  ", Observaciones: "  Condiciones equivalentes  ");

        var resultado = await servicio.GuardarAsync(Valido(parametros: parametros));

        Assert.True(resultado.Exito);
        var parametrosGuardados = repo.Guardada!.Parametros.ToDictionary(p => p.Clave, p => p.Valor);
        Assert.Equal(new[] { "WarmUp", "UsuariosVirtuales", "DuracionSegundos", "Observaciones" }.Order(), parametrosGuardados.Keys.Order());
        Assert.Equal("50", parametrosGuardados["UsuariosVirtuales"]);
        Assert.Equal("Condiciones equivalentes", parametrosGuardados["Observaciones"]);
        Assert.DoesNotContain("RampUpSegundos", parametrosGuardados.Keys);
        Assert.DoesNotContain("Intervalo", parametrosGuardados.Keys);
    }

    [Fact]
    public async Task ErrorDePersistencia_DevuelveMensajeSinDetalleTecnico()
    {
        var (servicio, _) = Crear(new FakeConfigRepo { Error = new InvalidOperationException("detalle interno") });

        var resultado = await servicio.GuardarAsync(Valido());

        Assert.False(resultado.Exito);
        Assert.DoesNotContain("detalle interno", resultado.Mensaje);
    }
}
