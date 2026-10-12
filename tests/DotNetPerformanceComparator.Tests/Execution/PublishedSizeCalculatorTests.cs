using DotNetPerformanceComparator.Infrastructure.Execution;

namespace DotNetPerformanceComparator.Tests.Execution;

public sealed class PublishedSizeCalculatorTests : IDisposable
{
    private readonly string _raiz = Directory.CreateTempSubdirectory("dnpc-size-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_raiz, recursive: true); } catch { /* best-effort */ }
    }

    private string Archivo(string nombreRelativo, int bytes)
    {
        var ruta = Path.Combine(_raiz, nombreRelativo);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllBytes(ruta, new byte[bytes]);
        return ruta;
    }

    [Fact]
    public void CarpetaVacia_EsCero()
    {
        Assert.Equal(0, PublishedSizeCalculator.CalculateBytes(_raiz));
    }

    [Fact]
    public void DirectorioInexistente_EsCero()
    {
        Assert.Equal(0, PublishedSizeCalculator.CalculateBytes(Path.Combine(_raiz, "no-existe")));
    }

    [Fact]
    public void UnArchivo_TamanoExacto()
    {
        Archivo("app.dll", 1234);
        Assert.Equal(1234, PublishedSizeCalculator.CalculateBytes(_raiz));
    }

    [Fact]
    public void VariosArchivos_SumaExacta()
    {
        Archivo("app.dll", 1000);
        Archivo("app.deps.json", 500);
        Archivo("app.runtimeconfig.json", 250);
        Assert.Equal(1750, PublishedSizeCalculator.CalculateBytes(_raiz));
    }

    [Fact]
    public void Subdirectorios_SumaRecursiva()
    {
        Archivo("app.dll", 1000);
        Archivo("wwwroot/site.css", 300);
        Archivo("wwwroot/js/app.js", 200);
        Assert.Equal(1500, PublishedSizeCalculator.CalculateBytes(_raiz));
    }

    [Fact]
    public void BytesToMb_Convierte()
    {
        Assert.Equal(15m, PublishedSizeCalculator.BytesToMb(15 * 1024 * 1024));
        Assert.Equal(0.5m, PublishedSizeCalculator.BytesToMb(512 * 1024));
    }

    [Fact]
    public void ArchivoCreadoTrasCongelar_NoCambiaElValorYaCapturado()
    {
        Archivo("app.dll", 1000);
        var congelado = PublishedSizeCalculator.CalculateBytes(_raiz); // se congela aquí (antes de iniciar la app)

        Archivo("runtime.log", 4096); // archivo de runtime creado después

        Assert.Equal(1000, congelado); // el valor capturado no cambia
        Assert.Equal(5096, PublishedSizeCalculator.CalculateBytes(_raiz)); // recalcular sí lo vería, pero no se recalcula
    }
}
