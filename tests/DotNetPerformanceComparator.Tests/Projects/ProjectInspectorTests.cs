using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Infrastructure.Projects;

namespace DotNetPerformanceComparator.Tests.Projects;

public sealed class ProjectInspectorTests : IDisposable
{
    private const string WebNet8 = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private readonly string _root = Directory.CreateTempSubdirectory("dnpc-inspector-").FullName;
    private readonly ProjectInspector _inspector = new();

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string CrearArchivo(string nombre, string contenido)
    {
        var ruta = Path.Combine(_root, nombre);
        File.WriteAllText(ruta, contenido);
        return ruta;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RutaVacia_EsRechazada(string? ruta)
    {
        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(ProjectFileStatus.RutaVacia, data.FileStatus);
    }

    [Fact]
    public async Task RutaRelativa_EsRechazada()
    {
        var data = await _inspector.InspectAsync(@"MiProyecto\MiProyecto.csproj");

        Assert.Equal(ProjectFileStatus.RutaInvalida, data.FileStatus);
    }

    [Fact]
    public async Task RutaInexistente_EsRechazada()
    {
        var data = await _inspector.InspectAsync(Path.Combine(_root, "NoExiste.csproj"));

        Assert.Equal(ProjectFileStatus.NoExiste, data.FileStatus);
    }

    [Fact]
    public async Task ExtensionDistintaDeCsproj_EsRechazada()
    {
        var ruta = CrearArchivo("Proyecto.txt", WebNet8);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(ProjectFileStatus.ExtensionInvalida, data.FileStatus);
    }

    [Theory]
    [InlineData("esto no es xml")]
    [InlineData("<Otro></Otro>")]
    public async Task ContenidoInvalido_EsRechazado(string contenido)
    {
        var ruta = CrearArchivo("Invalido.csproj", contenido);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(ProjectFileStatus.ContenidoInvalido, data.FileStatus);
    }

    [Fact]
    public async Task XmlConDtd_EsRechazadoSinProcesarlo()
    {
        var ruta = CrearArchivo("Dtd.csproj", """
            <?xml version="1.0"?>
            <!DOCTYPE Project [ <!ENTITY x "y"> ]>
            <Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>
            """);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(ProjectFileStatus.ContenidoInvalido, data.FileStatus);
    }

    [Fact]
    public async Task ProyectoWebNet8_DetectaSdkFrameworkYNombre()
    {
        var ruta = CrearArchivo("MiAplicacion.csproj", WebNet8);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(ProjectFileStatus.Leido, data.FileStatus);
        Assert.Equal("MiAplicacion", data.ProjectName);
        Assert.Equal(Path.GetFullPath(ruta), data.FullPath);
        Assert.Contains("Microsoft.NET.Sdk.Web", data.Sdks);
        Assert.Equal(new[] { "net8.0" }, data.TargetFrameworks);
    }

    [Fact]
    public async Task ProyectoMultitarget_DevuelveTodosLosFrameworks()
    {
        var ruta = CrearArchivo("Multi.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFrameworks>net8.0; net9.0</TargetFrameworks></PropertyGroup>
            </Project>
            """);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(new[] { "net8.0", "net9.0" }, data.TargetFrameworks);
    }

    [Fact]
    public async Task SdkConVersion_SeNormaliza()
    {
        var ruta = CrearArchivo("Versionado.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web/8.0.100"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>
            """);

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(new[] { "Microsoft.NET.Sdk.Web" }, data.Sdks);
    }

    [Fact]
    public async Task DetectaSoloCarpetasYArchivosExistentesEnLaRaiz()
    {
        var ruta = CrearArchivo("App.csproj", WebNet8);
        CrearArchivo("Program.cs", "// programa");
        CrearArchivo("appsettings.json", "{}");
        Directory.CreateDirectory(Path.Combine(_root, "Pages"));
        Directory.CreateDirectory(Path.Combine(_root, "wwwroot"));
        Directory.CreateDirectory(Path.Combine(_root, "OtraCarpeta"));
        Directory.CreateDirectory(Path.Combine(_root, "Sub", "Controllers"));

        var data = await _inspector.InspectAsync(ruta);

        Assert.Equal(new[] { "Pages", "wwwroot" }, data.KeyFolders);
        Assert.Equal(new[] { "App.csproj", "Program.cs", "appsettings.json" }, data.DetectedFiles);
    }

    [Fact]
    public async Task Inspeccion_NoModificaElArchivoDeProyecto()
    {
        var ruta = CrearArchivo("App.csproj", WebNet8);
        var escrituraAntes = File.GetLastWriteTimeUtc(ruta);

        await _inspector.InspectAsync(ruta);

        Assert.Equal(WebNet8, File.ReadAllText(ruta));
        Assert.Equal(escrituraAntes, File.GetLastWriteTimeUtc(ruta));
    }
}
