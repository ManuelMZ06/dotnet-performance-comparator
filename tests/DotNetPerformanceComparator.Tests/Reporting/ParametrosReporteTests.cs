using DotNetPerformanceComparator.Application.Evaluations.Reporting;
using DotNetPerformanceComparator.Application.Evaluations.Results;

namespace DotNetPerformanceComparator.Tests.Reporting;

/// <summary>
/// Pruebas del formateo de parámetros del reporte (CU-09, C-12.2.1): la ausencia se muestra como
/// "No disponible", nunca como 0; un 0 real sí se muestra como 0.
/// </summary>
public class ParametrosReporteTests
{
    [Fact]
    public void Carga_Validos_MuestranValorYUnidad()
    {
        var lineas = ParametrosReporte.Lineas(new ParametrosCargaConsulta(50, 30, 5, 10, 100));
        Assert.Equal(("Usuarios virtuales", "50 usuarios"), lineas[0]);
        Assert.Equal(("Duración", "30 s"), lineas[1]);
        Assert.Equal(("Intervalo entre solicitudes", "100 ms"), lineas[4]);
    }

    [Fact]
    public void Estres_Validos_SeisParametros()
    {
        var lineas = ParametrosReporte.Lineas(new ParametrosEstresConsulta(5, 5, 20, 15, 3, 50));
        Assert.Equal(6, lineas.Count);
        Assert.Equal(("Usuarios máximos", "20 usuarios"), lineas[2]);
    }

    [Fact]
    public void Concurrencia_Validos_CuatroParametros()
    {
        var lineas = ParametrosReporte.Lineas(new ParametrosConcurrenciaConsulta(10, 4, 2, 500));
        Assert.Equal(4, lineas.Count);
        Assert.Equal(("Rondas", "4 rondas"), lineas[1]);
        Assert.Equal(("Intervalo entre rondas", "500 ms"), lineas[3]);
    }

    [Fact]
    public void Ausente_EsNoDisponible_NoCero()
    {
        var lineas = ParametrosReporte.Lineas(new ParametrosCargaConsulta(40, null, null, null, 100));
        Assert.Equal(("Usuarios virtuales", "40 usuarios"), lineas[0]);
        Assert.Equal(("Duración", ParametrosReporte.NoDisponible), lineas[1]); // null → "No disponible"
        Assert.DoesNotContain(lineas, l => l.Valor == "0 s"); // nunca 0 ficticio
    }

    [Fact]
    public void CeroReal_SeMuestraComoCero()
    {
        var lineas = ParametrosReporte.Lineas(new ParametrosCargaConsulta(40, 30, 0, 0, 100));
        Assert.Equal(("Warm-up", "0 s"), lineas[2]);   // 0 real sí se muestra
        Assert.Equal(("Ramp-up", "0 s"), lineas[3]);
    }

    [Fact]
    public void SinParametros_ListaVacia()
    {
        Assert.Empty(ParametrosReporte.Lineas(null));
    }
}
