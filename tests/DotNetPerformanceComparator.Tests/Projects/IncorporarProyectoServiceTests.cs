using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Projects;

public class IncorporarProyectoServiceTests
{
    private const string Ruta = @"C:\Temp\MiAplicacion\MiAplicacion.csproj";
    private static readonly DateTimeOffset Ahora = new(2026, 10, 7, 15, 30, 0, TimeSpan.Zero);

    private sealed class FakeInspector(ProjectInspectionData data) : IProjectInspector
    {
        public Task<ProjectInspectionData> InspectAsync(string? csprojPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(data);
    }

    private sealed class FakeRepository : IProyectoDotNetRepository
    {
        public List<ProyectoDotNet> Agregados { get; } = [];
        public Exception? Error { get; init; }

        public Task AddAsync(ProyectoDotNet proyecto, CancellationToken cancellationToken = default)
        {
            if (Error is not null)
            {
                throw Error;
            }

            proyecto.Id = Agregados.Count + 1;
            Agregados.Add(proyecto);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProyectoDotNet>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>(Agregados);

        public Task<ProyectoDotNet?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Agregados.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static ProjectInspectionData Leido(string[] sdks, string[] tfms) =>
        new(ProjectFileStatus.Leido, Ruta, "MiAplicacion", sdks, tfms, ["Pages", "wwwroot"], ["MiAplicacion.csproj", "Program.cs"]);

    private static ProjectInspectionData Valido() => Leido(["Microsoft.NET.Sdk.Web"], ["net8.0"]);

    private static (IncorporarProyectoService Servicio, FakeRepository Repositorio) Crear(ProjectInspectionData data, FakeRepository? repositorio = null)
    {
        repositorio ??= new FakeRepository();
        var servicio = new IncorporarProyectoService(
            new FakeInspector(data),
            repositorio,
            new FixedTimeProvider(Ahora),
            NullLogger<IncorporarProyectoService>.Instance);
        return (servicio, repositorio);
    }

    private static ValidationStatus[] Estados(ProjectInspectionResult r) => r.Validaciones.Select(v => v.Estado).ToArray();

    [Fact]
    public async Task ProyectoWebNet8_EsValidoConInformacionDetectada()
    {
        var (servicio, _) = Crear(Valido());

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.True(resultado.EsValido);
        Assert.Equal("MiAplicacion", resultado.NombreSugerido);
        Assert.Equal("ASP.NET Core Web", resultado.TipoAplicacion);
        Assert.Equal(".NET 8", resultado.VersionDetectada);
        Assert.Equal("net8.0", resultado.FrameworkObjetivo);
        Assert.Equal(new[] { "Pages", "wwwroot" }, resultado.CarpetasClave);
        Assert.All(resultado.Validaciones, v => Assert.Equal(ValidationStatus.Correcto, v.Estado));
    }

    [Fact]
    public async Task ProyectoMultitargetConNet8_EsValido()
    {
        var (servicio, _) = Crear(Leido(["Microsoft.NET.Sdk.Web"], ["net8.0", "net9.0"]));

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.True(resultado.EsValido);
        Assert.Equal("net8.0", resultado.FrameworkObjetivo);
    }

    [Fact]
    public async Task ProyectoNoWeb_NoEsValido()
    {
        var (servicio, _) = Crear(Leido(["Microsoft.NET.Sdk"], ["net8.0"]));

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.False(resultado.EsValido);
        Assert.Null(resultado.TipoAplicacion);
        Assert.Equal(new[] { ValidationStatus.Correcto, ValidationStatus.Error, ValidationStatus.Correcto, ValidationStatus.Error }, Estados(resultado));
        Assert.Contains("aplicación web ASP.NET Core", resultado.Mensaje);
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net7.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public async Task ProyectoWebConFrameworkDistinto_NoEsValido(string tfm)
    {
        var (servicio, _) = Crear(Leido(["Microsoft.NET.Sdk.Web"], [tfm]));

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.False(resultado.EsValido);
        Assert.Null(resultado.VersionDetectada);
        Assert.Equal(tfm, resultado.FrameworkObjetivo);
        Assert.Equal(new[] { ValidationStatus.Correcto, ValidationStatus.Correcto, ValidationStatus.Error, ValidationStatus.Error }, Estados(resultado));
        Assert.Contains("diferente de .NET 8", resultado.Mensaje);
    }

    [Fact]
    public async Task ProyectoSinTargetFramework_NoEsValido()
    {
        var (servicio, _) = Crear(Leido(["Microsoft.NET.Sdk.Web"], []));

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.False(resultado.EsValido);
        Assert.Contains("TargetFramework", resultado.Mensaje);
    }

    [Theory]
    [InlineData(ProjectFileStatus.NoExiste, ValidationStatus.Error)]
    [InlineData(ProjectFileStatus.ExtensionInvalida, ValidationStatus.Error)]
    [InlineData(ProjectFileStatus.ContenidoInvalido, ValidationStatus.Correcto)]
    [InlineData(ProjectFileStatus.AccesoDenegado, ValidationStatus.Correcto)]
    public async Task ArchivoNoLeido_NoEsValidoYNoEvaluaLoDemas(ProjectFileStatus status, ValidationStatus archivo)
    {
        var (servicio, _) = Crear(ProjectInspectionData.Failed(status, Ruta));

        var resultado = await servicio.InspeccionarAsync(Ruta);

        Assert.False(resultado.EsValido);
        Assert.Equal(new[] { archivo, ValidationStatus.Pendiente, ValidationStatus.Pendiente, ValidationStatus.Error }, Estados(resultado));
        Assert.False(string.IsNullOrWhiteSpace(resultado.Mensaje));
    }

    [Fact]
    public async Task Registrar_InspeccionInvalida_NoPersiste()
    {
        var (servicio, repositorio) = Crear(Leido(["Microsoft.NET.Sdk.Web"], ["net7.0"]));

        var resultado = await servicio.RegistrarAsync(new IncorporarProyectoRequest("MiAplicacion", Ruta));

        Assert.False(resultado.Exito);
        Assert.Empty(repositorio.Agregados);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Registrar_SinNombre_NoPersiste(string? nombre)
    {
        var (servicio, repositorio) = Crear(Valido());

        var resultado = await servicio.RegistrarAsync(new IncorporarProyectoRequest(nombre, Ruta));

        Assert.False(resultado.Exito);
        Assert.Empty(repositorio.Agregados);
    }

    [Fact]
    public async Task Registrar_ProyectoValido_PersisteLosCamposDelModelo()
    {
        var (servicio, repositorio) = Crear(Valido());

        var resultado = await servicio.RegistrarAsync(new IncorporarProyectoRequest("  Mi App  ", Ruta));

        Assert.True(resultado.Exito);
        Assert.Equal(1, resultado.ProyectoId);
        var proyecto = Assert.Single(repositorio.Agregados);
        Assert.Equal("Mi App", proyecto.Nombre);
        Assert.Equal(Ruta, proyecto.RutaOriginal);
        Assert.Equal(".NET 8", proyecto.VersionBase);
        Assert.Equal("Valido", proyecto.EstadoValidacion);
        Assert.Equal(Ahora.UtcDateTime, proyecto.FechaRegistro);
        Assert.Equal(DateTimeKind.Utc, proyecto.FechaRegistro.Kind);
        Assert.Empty(proyecto.Migraciones);
        Assert.Empty(proyecto.ConfiguracionesEvaluacion);
    }

    [Fact]
    public async Task Registrar_ErrorDePersistencia_DevuelveMensajeSinDetalleTecnico()
    {
        var repositorio = new FakeRepository { Error = new InvalidOperationException("detalle interno") };
        var (servicio, _) = Crear(Valido(), repositorio);

        var resultado = await servicio.RegistrarAsync(new IncorporarProyectoRequest("MiAplicacion", Ruta));

        Assert.False(resultado.Exito);
        Assert.DoesNotContain("detalle interno", resultado.Mensaje);
    }
}
