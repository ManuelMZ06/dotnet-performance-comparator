using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

namespace DotNetPerformanceComparator.Tests.Migrations;

public sealed class ProjectCopyServiceTests : IDisposable
{
    private readonly TempTree _tree = new();
    private readonly ProjectCopyService _servicio = new();
    private readonly string _origen;

    public ProjectCopyServiceTests()
    {
        _origen = _tree.Dir("Origen/App");
        _tree.File("Origen/App/App.csproj", TempTree.WebNet8);
        _tree.File("Origen/App/Program.cs", "// programa");
        _tree.File("Origen/App/appsettings.json", "{}");
        _tree.File("Origen/App/Pages/Index.cshtml", "<h1>Hola</h1>");
        _tree.File("Origen/App/wwwroot/css/site.css", "body {}");
        _tree.File("Origen/App/bin/Debug/App.dll", "binario");
        _tree.File("Origen/App/obj/project.assets.json", "{}");
        _tree.File("Origen/App/.vs/config.json", "{}");
        _tree.File("Origen/App/.git/HEAD", "ref");
        _tree.File("Origen/App/Modulo/obj/x.json", "{}");
        _tree.Dir("Salida");
    }

    public void Dispose() => _tree.Dispose();

    [Theory]
    [InlineData(null, DestinationStatus.RutaVacia)]
    [InlineData("  ", DestinationStatus.RutaVacia)]
    [InlineData(@"relativa\Salida", DestinationStatus.RutaInvalida)]
    public void Destino_RutaVaciaORelativa_EsRechazado(string? destino, DestinationStatus esperado)
    {
        Assert.Equal(esperado, _servicio.CheckDestination(_origen, destino).Status);
    }

    [Fact]
    public void Destino_IgualAlOrigen_EsRechazadoAunqueCambienMayusculasYSeparador()
    {
        var destino = _origen.ToUpperInvariant() + Path.DirectorySeparatorChar;

        Assert.Equal(DestinationStatus.IgualAlOrigen, _servicio.CheckDestination(_origen, destino).Status);
    }

    [Fact]
    public void Destino_DentroDelOrigen_EsRechazado()
    {
        Assert.Equal(DestinationStatus.DentroDelOrigen, _servicio.CheckDestination(_origen, Path.Combine(_origen, "Net10")).Status);
    }

    [Fact]
    public void Destino_QueContieneAlOrigen_EsRechazado()
    {
        Assert.Equal(DestinationStatus.ContieneAlOrigen, _servicio.CheckDestination(_origen, _tree.Path("Origen")).Status);
    }

    [Fact]
    public void Destino_NoVacio_EsRechazado()
    {
        _tree.File("Ocupado/existente.txt");

        Assert.Equal(DestinationStatus.NoVacio, _servicio.CheckDestination(_origen, _tree.Path("Ocupado")).Status);
    }

    [Fact]
    public void Destino_ConPadreInexistente_EsRechazado()
    {
        Assert.Equal(DestinationStatus.PadreNoExiste, _servicio.CheckDestination(_origen, _tree.Path("NoExiste", "Salida")).Status);
    }

    [Fact]
    public void Destino_Archivo_EsRechazado()
    {
        var archivo = _tree.File("archivo.txt");

        Assert.Equal(DestinationStatus.EsArchivo, _servicio.CheckDestination(_origen, archivo).Status);
    }

    [Fact]
    public void Destino_Nuevo_EsValido()
    {
        var check = _servicio.CheckDestination(_origen, _tree.Path("Salida", "AppNet10"));

        Assert.True(check.IsValid);
        Assert.False(check.Existed);
    }

    [Fact]
    public void Destino_ExistenteVacio_EsValido()
    {
        var check = _servicio.CheckDestination(_origen, _tree.Dir("Vacio"));

        Assert.True(check.IsValid);
        Assert.True(check.Existed);
    }

    [Fact]
    public async Task Copia_ConservaCodigoYExcluyeArtefactos()
    {
        var destino = _tree.Path("Salida", "AppNet10");
        var huellaAntes = TempTree.Huella(_origen);

        var resultado = await _servicio.CopyAsync(_origen, _servicio.CheckDestination(_origen, destino));

        Assert.True(resultado.Success);
        Assert.Equal(5, resultado.FilesCopied);
        Assert.Equal(TempTree.WebNet8, File.ReadAllText(Path.Combine(destino, "App.csproj")));
        Assert.True(File.Exists(Path.Combine(destino, "Program.cs")));
        Assert.True(File.Exists(Path.Combine(destino, "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(destino, "Pages", "Index.cshtml")));
        Assert.True(File.Exists(Path.Combine(destino, "wwwroot", "css", "site.css")));
        Assert.False(Directory.Exists(Path.Combine(destino, "bin")));
        Assert.False(Directory.Exists(Path.Combine(destino, "obj")));
        Assert.False(Directory.Exists(Path.Combine(destino, ".vs")));
        Assert.False(Directory.Exists(Path.Combine(destino, ".git")));
        Assert.False(Directory.Exists(Path.Combine(destino, "Modulo", "obj")));
        Assert.Equal(huellaAntes, TempTree.Huella(_origen));
    }

    [Fact]
    public async Task Copia_EnDirectorioExistenteVacio_LoConserva()
    {
        var destino = _tree.Dir("Vacio");

        var resultado = await _servicio.CopyAsync(_origen, _servicio.CheckDestination(_origen, destino));

        Assert.True(resultado.Success);
        Assert.True(File.Exists(Path.Combine(destino, "App.csproj")));
    }

    [Fact]
    public async Task CopiaCancelada_EliminaElDestinoCreadoPorLaOperacion()
    {
        var destino = _tree.Path("Salida", "Cancelada");
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        var resultado = await _servicio.CopyAsync(_origen, _servicio.CheckDestination(_origen, destino), cancelacion.Token);

        Assert.False(resultado.Success);
        Assert.False(Directory.Exists(destino));
    }

    [Fact]
    public async Task CopiaCancelada_NoEliminaUnDirectorioQueYaExistia()
    {
        var destino = _tree.Dir("Vacio");
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        var resultado = await _servicio.CopyAsync(_origen, _servicio.CheckDestination(_origen, destino), cancelacion.Token);

        Assert.False(resultado.Success);
        Assert.True(Directory.Exists(destino));
        Assert.Empty(Directory.EnumerateFileSystemEntries(destino));
    }
}
