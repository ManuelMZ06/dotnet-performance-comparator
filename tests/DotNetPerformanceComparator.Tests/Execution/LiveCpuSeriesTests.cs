using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Execution;

namespace DotNetPerformanceComparator.Tests.Execution;

public class LiveCpuSeriesTests
{
    [Fact]
    public void MismaEjecucion_AcumulaPuntos()
    {
        var serie = new LiveCpuSeries();
        serie.Observar(VersionesDotNet.Net8, 1, 0.2m);
        serie.Observar(VersionesDotNet.Net8, 1, 0.3m);
        serie.Observar(VersionesDotNet.Net8, 1, 0.25m);

        var puntos = serie.Snapshot();
        Assert.Equal(3, puntos.Count);
        Assert.Equal(new[] { 0.0, 1.0, 2.0 }, puntos.Select(p => p.X).ToArray()); // X = muestras sucesivas
        Assert.Equal(0.3, puntos[1].Y);
    }

    [Fact]
    public void CambioDeVersion_ReiniciaLaSerie()
    {
        var serie = new LiveCpuSeries();
        serie.Observar(VersionesDotNet.Net8, 1, 0.2m);
        serie.Observar(VersionesDotNet.Net8, 1, 0.3m);
        serie.Observar(VersionesDotNet.Net10, 1, 0.9m); // nueva Ejecucion (otra versión)

        var puntos = serie.Snapshot();
        Assert.Single(puntos);
        Assert.Equal(0.9, puntos[0].Y);
    }

    [Fact]
    public void CambioDeNumeroEjecucion_ReiniciaLaSerie()
    {
        var serie = new LiveCpuSeries();
        serie.Observar(VersionesDotNet.Net8, 1, 0.2m);
        serie.Observar(VersionesDotNet.Net8, 2, 0.5m); // otra repetición

        Assert.Single(serie.Snapshot());
    }

    [Fact]
    public void MismaEjecucionConMuchasMuestras_NoReiniciaPorNivelesORondas()
    {
        // Niveles/rondas pertenecen a la misma Ejecucion (misma versión y repetición) → la serie continúa.
        var serie = new LiveCpuSeries();
        for (var i = 0; i < 10; i++)
        {
            serie.Observar(VersionesDotNet.Net8, 1, 0.1m * i);
        }

        Assert.Equal(10, serie.Snapshot().Count);
    }
}
