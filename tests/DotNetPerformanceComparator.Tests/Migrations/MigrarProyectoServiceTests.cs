using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Entities;
using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Migrations;

public sealed class MigrarProyectoServiceTests : IDisposable
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    private const string Sdk = "10.0.400";

    private readonly TempTree _tree = new();
    private readonly string _csproj;
    private readonly string _destino;

    public MigrarProyectoServiceTests()
    {
        _csproj = _tree.File("Base/App/App.csproj", TempTree.WebNet8);
        _tree.File("Base/App/Program.cs", "// programa");
        _tree.File("Base/App/obj/project.assets.json", "{}");
        _tree.Dir("Salida");
        _destino = _tree.Path("Salida", "AppNet10");
    }

    public void Dispose() => _tree.Dispose();

    // ---------------------------------------------------------------- fakes

    private sealed class FakeProyectos(params ProyectoDotNet[] proyectos) : IProyectoDotNetRepository
    {
        public Task AddAsync(ProyectoDotNet proyecto, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ProyectoDotNet>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProyectoDotNet>>(proyectos);

        public Task<ProyectoDotNet?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(proyectos.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeIncorporacion(bool valido) : IIncorporarProyectoService
    {
        public Task<ProjectInspectionResult> InspeccionarAsync(string? rutaProyecto, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProjectInspectionResult(
                rutaProyecto, "App", valido ? "ASP.NET Core Web" : null, valido ? ".NET 8" : null, valido ? "net8.0" : "net7.0", [], [],
                valido ? ProjectInspectionState.Valido : ProjectInspectionState.NoValido, [],
                valido ? "Cumple." : "El proyecto utiliza un framework diferente de .NET 8 (net7.0)."));

        public Task<IncorporarProyectoResult> RegistrarAsync(IncorporarProyectoRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>Simula dotnet: --list-sdks, --version, restore y build.</summary>
    private sealed class FakeDotNet : IDotNetCommandRunner
    {
        public string[] Sdks { get; init; } = ["8.0.425 [C:\\sdk]", $"{Sdk} [C:\\sdk]"];
        public int RestoreExit { get; init; }
        public int BuildExit { get; init; }
        public List<(string[] Args, string? Sdk)> Llamadas { get; } = [];

        public Task<DotNetCommandResult> RunAsync(IReadOnlyList<string> arguments, string? sdkVersion, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
        {
            Llamadas.Add((arguments.ToArray(), sdkVersion));
            DotNetCommandResult resultado = arguments[0] switch
            {
                "--list-sdks" => new(true, 0, Sdks),
                "--version" => new(true, 0, [sdkVersion ?? "8.0.425"]),
                "restore" => new(true, RestoreExit, RestoreExit == 0 ? ["Restauración correcta."] : ["error NU1101: No se encontró el paquete X."]),
                "build" => new(true, BuildExit, BuildExit == 0 ? ["Compilación correcta."] : ["Program.cs(3,1): error CS0246: No se encontró el tipo 'Foo'."]),
                _ => new(false, -1, [])
            };

            foreach (var linea in resultado.Output)
            {
                onOutputLine?.Invoke(linea);
            }

            return Task.FromResult(resultado);
        }
    }

    private sealed class FakeCopiaFallida : IProjectCopyService
    {
        private readonly ProjectCopyService _real = new();

        public DestinationCheck CheckDestination(string sourceDirectory, string? destination) => _real.CheckDestination(sourceDirectory, destination);

        public Task<CopyResult> CopyAsync(string sourceDirectory, DestinationCheck destination, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CopyResult(false, 0, [], "Se denegó el acceso a un archivo o directorio durante la copia."));
    }

    private sealed class FakeMigraciones : IMigracionRepository
    {
        public List<string> Estados { get; } = [];
        public Migracion? Ultima { get; private set; }

        public Task AddAsync(Migracion migracion, CancellationToken cancellationToken = default)
        {
            migracion.Id = 7;
            Registrar(migracion);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Migracion migracion, CancellationToken cancellationToken = default)
        {
            Registrar(migracion);
            return Task.CompletedTask;
        }

        private void Registrar(Migracion m)
        {
            Estados.Add(m.Estado);
            Ultima = new Migracion
            {
                Id = m.Id, ProyectoId = m.ProyectoId, VersionOrigen = m.VersionOrigen, VersionDestino = m.VersionDestino,
                RutaMigrada = m.RutaMigrada, Estado = m.Estado, FechaInicio = m.FechaInicio, FechaFin = m.FechaFin, Detalle = m.Detalle
            };
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ListaProgreso : IProgress<MigrationProgress>
    {
        public List<MigrationProgress> Items { get; } = [];

        public void Report(MigrationProgress value) => Items.Add(value);

        public IEnumerable<(MigrationStage, MigrationStageStatus)> Etapas =>
            Items.Where(i => i.Stage is not null).Select(i => (i.Stage!.Value, i.StageStatus));

        public MigrationStageStatus EstadoFinal(MigrationStage etapa) =>
            Etapas.Where(e => e.Item1 == etapa).Select(e => e.Item2).DefaultIfEmpty(MigrationStageStatus.Pendiente).Last();
    }

    private ProyectoDotNet Proyecto() => new()
    {
        Id = 1, Nombre = "App", RutaOriginal = _csproj, VersionBase = ".NET 8", EstadoValidacion = "Valido", FechaRegistro = Ahora.UtcDateTime
    };

    private (MigrarProyectoService Servicio, FakeDotNet DotNet, FakeMigraciones Migraciones) Crear(
        bool valido = true,
        FakeDotNet? dotnet = null,
        IProjectCopyService? copia = null,
        params ProyectoDotNet[] proyectos)
    {
        dotnet ??= new FakeDotNet();
        var migraciones = new FakeMigraciones();
        var servicio = new MigrarProyectoService(
            new FakeProyectos(proyectos.Length > 0 ? proyectos : [Proyecto()]),
            new FakeIncorporacion(valido),
            new ProjectFileEditor(),
            copia ?? new ProjectCopyService(),
            dotnet,
            migraciones,
            new FixedTime(Ahora),
            NullLogger<MigrarProyectoService>.Instance);
        return (servicio, dotnet, migraciones);
    }

    // ---------------------------------------------------------------- pruebas

    [Fact]
    public async Task ObtenerProyectos_DevuelveNombreYRuta()
    {
        var (servicio, _, _) = Crear();

        var proyecto = Assert.Single(await servicio.ObtenerProyectosAsync());

        Assert.Equal(1, proyecto.Id);
        Assert.Equal("App", proyecto.Nombre);
    }

    [Fact]
    public async Task ProyectoInexistente_NoCreaMigracionNiCopia()
    {
        var (servicio, _, migraciones) = Crear();
        var progreso = new ListaProgreso();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(99, _destino), progreso);

        Assert.False(resultado.Exito);
        Assert.Null(resultado.Migracion);
        Assert.Empty(migraciones.Estados);
        Assert.False(Directory.Exists(_destino));
        Assert.Equal(MigrationStageStatus.Error, progreso.EstadoFinal(MigrationStage.ValidacionProyecto));
    }

    [Fact]
    public async Task ProyectoQueDejoDeSerValido_NoCreaMigracionNiCopia()
    {
        var (servicio, _, migraciones) = Crear(valido: false);

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino));

        Assert.False(resultado.Exito);
        Assert.Contains("ya no cumple", resultado.Mensaje);
        Assert.Empty(migraciones.Estados);
        Assert.False(Directory.Exists(_destino));
    }

    [Fact]
    public async Task SinSdk10_NoCreaMigracionNiCopia()
    {
        var (servicio, dotnet, migraciones) = Crear(dotnet: new FakeDotNet { Sdks = ["8.0.425 [C:\\sdk]"] });

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino));

        Assert.False(resultado.Exito);
        Assert.Contains("SDK .NET 10", resultado.Mensaje);
        Assert.Empty(migraciones.Estados);
        Assert.False(Directory.Exists(_destino));
        Assert.DoesNotContain(dotnet.Llamadas, l => l.Args[0] is "restore" or "build");
    }

    [Fact]
    public async Task DestinoInvalido_NoCreaMigracion()
    {
        var (servicio, _, migraciones) = Crear();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, Path.Combine(Path.GetDirectoryName(_csproj)!, "Net10")));

        Assert.False(resultado.Exito);
        Assert.Contains("dentro del proyecto original", resultado.Mensaje);
        Assert.Empty(migraciones.Estados);
    }

    [Fact]
    public async Task ReferenciaExterna_DetieneAntesDeCopiar()
    {
        File.WriteAllText(_csproj, TempTree.WebNet8.Replace("</Project>",
            """<ItemGroup><ProjectReference Include="..\..\Compartido\Lib.csproj" /></ItemGroup></Project>"""));
        var (servicio, _, migraciones) = Crear();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino));

        Assert.False(resultado.Exito);
        Assert.Contains("fuera de su directorio", resultado.Mensaje);
        Assert.Empty(migraciones.Estados);
        Assert.False(Directory.Exists(_destino));
    }

    [Fact]
    public async Task CopiaFallida_RegistraFallidaSinRutaMigrada()
    {
        var (servicio, dotnet, migraciones) = Crear(copia: new FakeCopiaFallida());
        var progreso = new ListaProgreso();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino), progreso);

        Assert.False(resultado.Exito);
        Assert.Equal(["EnProceso", "Fallida"], migraciones.Estados);
        Assert.Null(migraciones.Ultima!.RutaMigrada);
        Assert.Equal(MigrationStageStatus.Error, progreso.EstadoFinal(MigrationStage.CopiaExperimental));
        Assert.Equal(MigrationStageStatus.Pendiente, progreso.EstadoFinal(MigrationStage.ActualizacionFramework));
        Assert.DoesNotContain(dotnet.Llamadas, l => l.Args[0] is "restore" or "build");
    }

    [Fact]
    public async Task RestoreFallido_RegistraFallidaConservaCopiaYNoCompila()
    {
        var (servicio, dotnet, migraciones) = Crear(dotnet: new FakeDotNet { RestoreExit = 1 });
        var progreso = new ListaProgreso();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino), progreso);

        Assert.False(resultado.Exito);
        Assert.Equal("Fallida", migraciones.Ultima!.Estado);
        Assert.Equal(_destino, migraciones.Ultima.RutaMigrada);
        Assert.Contains("NU1101", migraciones.Ultima.Detalle);
        Assert.Equal(Ahora.UtcDateTime, migraciones.Ultima.FechaFin);
        Assert.True(File.Exists(Path.Combine(_destino, "App.csproj")));
        Assert.DoesNotContain(dotnet.Llamadas, l => l.Args[0] == "build");
        Assert.Equal(MigrationStageStatus.Error, progreso.EstadoFinal(MigrationStage.Restauracion));
        Assert.Equal(MigrationStageStatus.Pendiente, progreso.EstadoFinal(MigrationStage.Build));
    }

    [Fact]
    public async Task BuildFallido_RegistraFallidaConDetalleComprensible()
    {
        var (servicio, _, migraciones) = Crear(dotnet: new FakeDotNet { BuildExit = 1 });
        var progreso = new ListaProgreso();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino), progreso);

        Assert.False(resultado.Exito);
        Assert.Equal("Fallida", migraciones.Ultima!.Estado);
        Assert.Contains("CS0246", migraciones.Ultima.Detalle);
        Assert.DoesNotContain("   at ", migraciones.Ultima.Detalle);
        Assert.Equal(MigrationStageStatus.Error, progreso.EstadoFinal(MigrationStage.Build));
        Assert.Equal(MigrationStageStatus.Correcto, progreso.EstadoFinal(MigrationStage.Restauracion));
    }

    [Fact]
    public async Task MigracionExitosa_CopiaActualizaRestauraYCompilaConSdk10()
    {
        var (servicio, dotnet, migraciones) = Crear();
        var progreso = new ListaProgreso();

        var resultado = await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino), progreso);

        Assert.True(resultado.Exito);
        Assert.Equal(["EnProceso", "Exitosa"], migraciones.Estados);
        var m = migraciones.Ultima!;
        Assert.Equal(1, m.ProyectoId);
        Assert.Equal(".NET 8", m.VersionOrigen);
        Assert.Equal(".NET 10", m.VersionDestino);
        Assert.Equal(_destino, m.RutaMigrada);
        Assert.Equal(Ahora.UtcDateTime, m.FechaInicio);
        Assert.Equal(Ahora.UtcDateTime, m.FechaFin);
        Assert.Equal(Sdk, resultado.Migracion!.SdkUtilizado);

        var copia = File.ReadAllText(Path.Combine(_destino, "App.csproj"));
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", copia);
        Assert.Contains("""<PackageReference Include="Serilog" Version="3.1.1" />""", copia);
        Assert.False(Directory.Exists(Path.Combine(_destino, "obj")));

        var restore = Assert.Single(dotnet.Llamadas, l => l.Args[0] == "restore");
        var build = Assert.Single(dotnet.Llamadas, l => l.Args[0] == "build");
        Assert.Equal(Sdk, restore.Sdk);
        Assert.Equal(Sdk, build.Sdk);
        Assert.Contains("--no-restore", build.Args);
        Assert.Contains("net10.0", build.Args);
        Assert.Equal(Path.Combine(_destino, "App.csproj"), restore.Args[1]);

        Assert.All(MigrationStages.All, e => Assert.Equal(MigrationStageStatus.Correcto, progreso.EstadoFinal(e)));
        Assert.Contains(progreso.Items, i => i.LogLine == "Compilación correcta.");
    }

    [Fact]
    public async Task Migracion_NoModificaElProyectoOriginal()
    {
        var directorioOriginal = Path.GetDirectoryName(_csproj)!;
        var huella = TempTree.Huella(directorioOriginal);
        var (servicio, _, _) = Crear(dotnet: new FakeDotNet { BuildExit = 1 });

        await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino));

        Assert.Equal(huella, TempTree.Huella(directorioOriginal));
        Assert.Contains("<TargetFramework>net8.0</TargetFramework>", File.ReadAllText(_csproj));
    }

    [Fact]
    public async Task EtapasSeReportanEnOrden()
    {
        var (servicio, _, _) = Crear();
        var progreso = new ListaProgreso();

        await servicio.MigrarAsync(new MigrarProyectoRequest(1, _destino), progreso);

        var enProceso = progreso.Etapas.Where(e => e.Item2 == MigrationStageStatus.EnProceso).Select(e => e.Item1);
        Assert.Equal(MigrationStages.All, enProceso);
    }
}
