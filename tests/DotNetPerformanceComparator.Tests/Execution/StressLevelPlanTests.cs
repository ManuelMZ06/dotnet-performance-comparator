using DotNetPerformanceComparator.Application.Evaluations.Execution;

namespace DotNetPerformanceComparator.Tests.Execution;

public class StressLevelPlanTests
{
    [Fact]
    public void IncrementoExacto()
    {
        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, StressLevelPlan.Generar(10, 10, 50).ToArray());
    }

    [Fact]
    public void IncrementoNoExacto_UltimoEsElMaximo()
    {
        Assert.Equal(new[] { 10, 25, 40, 50 }, StressLevelPlan.Generar(10, 15, 50).ToArray());
    }

    [Fact]
    public void IncrementoMayorQueRango_SoloInicialYMaximo()
    {
        Assert.Equal(new[] { 10, 50 }, StressLevelPlan.Generar(10, 100, 50).ToArray());
    }

    [Fact]
    public void RangoMinimo()
    {
        Assert.Equal(new[] { 1, 2 }, StressLevelPlan.Generar(1, 1, 2).ToArray());
    }

    [Fact]
    public void IncrementoAterrizaExactoEnMaximo_SinDuplicado()
    {
        Assert.Equal(new[] { 10, 30, 50 }, StressLevelPlan.Generar(10, 20, 50).ToArray());
    }

    [Fact]
    public void NuncaSuperaElMaximo_YSinDuplicados()
    {
        var niveles = StressLevelPlan.Generar(7, 11, 100);
        Assert.Equal(100, niveles[^1]);              // último exacto
        Assert.All(niveles, n => Assert.True(n <= 100)); // nunca supera
        Assert.Equal(niveles.Count, niveles.Distinct().Count()); // sin duplicados
    }

    [Fact]
    public void IncrementoEnormeNoDesborda()
    {
        var niveles = StressLevelPlan.Generar(10, int.MaxValue, 50);
        Assert.Equal(new[] { 10, 50 }, niveles.ToArray()); // no overflow al sumar el incremento
    }
}
