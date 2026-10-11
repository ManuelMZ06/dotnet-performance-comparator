using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Tests.Execution;

public class EvaluationExecutionPlanTests
{
    private static (string, int) P(string v, int n) => (v, n);

    [Fact]
    public void Secuencia_1Ejecucion()
    {
        var pasos = EvaluationExecutionPlan.BuildSequence(1).Select(p => (p.VersionDotNet, p.NumeroEjecucion));
        Assert.Equal(new[] { P(VersionesDotNet.Net8, 1), P(VersionesDotNet.Net10, 1) }, pasos);
    }

    [Fact]
    public void Secuencia_2Ejecuciones_Alterna()
    {
        var pasos = EvaluationExecutionPlan.BuildSequence(2).Select(p => (p.VersionDotNet, p.NumeroEjecucion));
        Assert.Equal(
            new[]
            {
                P(VersionesDotNet.Net8, 1), P(VersionesDotNet.Net10, 1),
                P(VersionesDotNet.Net10, 2), P(VersionesDotNet.Net8, 2)
            },
            pasos);
    }

    [Fact]
    public void Secuencia_3Ejecuciones_Alterna()
    {
        var pasos = EvaluationExecutionPlan.BuildSequence(3).Select(p => (p.VersionDotNet, p.NumeroEjecucion));
        Assert.Equal(
            new[]
            {
                P(VersionesDotNet.Net8, 1), P(VersionesDotNet.Net10, 1),
                P(VersionesDotNet.Net10, 2), P(VersionesDotNet.Net8, 2),
                P(VersionesDotNet.Net8, 3), P(VersionesDotNet.Net10, 3)
            },
            pasos);
    }
}

public class ResourceAggregatorTests
{
    [Fact]
    public void Cpu_CalculaPorcentajePromedio()
    {
        // 2 procesadores; entre muestras: 1000 ms de CPU en 1000 ms de reloj → 50 %.
        var muestras = new[]
        {
            new ResourceSample(0, 100 * 1024 * 1024, 0),
            new ResourceSample(1000, 200 * 1024 * 1024, 1000),
            new ResourceSample(1500, 200 * 1024 * 1024, 2000) // 500 ms CPU / 1000 ms / 2 = 25 %
        };

        var agg = ResourceAggregator.Aggregate(muestras, processorCount: 2);

        Assert.Equal(2, agg.MuestrasCpu);
        Assert.Equal(37.5m, agg.CpuPorcentaje); // promedio de 50 % y 25 %
    }

    [Fact]
    public void Ram_PromediaYConvierteABytesAMb()
    {
        var muestras = new[]
        {
            new ResourceSample(0, 100 * 1024 * 1024, 0),
            new ResourceSample(1000, 300 * 1024 * 1024, 1000)
        };

        var agg = ResourceAggregator.Aggregate(muestras, 1);

        Assert.Equal(2, agg.MuestrasRam);
        Assert.Equal(200m, agg.MemoriaMb); // (100+300)/2
    }

    [Fact]
    public void Cpu_PequenoExcesoSobre100_SeAjustaA100()
    {
        var muestras = new[]
        {
            new ResourceSample(0, 1, 0),
            new ResourceSample(1020, 1, 1000) // 1020 ms CPU en 1000 ms con 1 proc → 102 % (dentro de tolerancia) → 100 %
        };

        var agg = ResourceAggregator.Aggregate(muestras, 1);
        Assert.Equal(1, agg.MuestrasCpu);
        Assert.Equal(100m, agg.CpuPorcentaje);
    }

    [Fact]
    public void Cpu_ExcesoGrandeSobre100_SeIgnoraComoInvalido()
    {
        var muestras = new[]
        {
            new ResourceSample(0, 1, 0),
            new ResourceSample(5000, 1, 1000) // 5000 ms CPU en 1000 ms con 1 proc → 500 % → muestra inválida, se descarta
        };

        var agg = ResourceAggregator.Aggregate(muestras, 1);
        Assert.Equal(0, agg.MuestrasCpu);
        Assert.Null(agg.CpuPorcentaje); // no se oculta el error con un clamp a 100
    }

    [Fact]
    public void SinIntervalosValidos_NoHayCpu()
    {
        var muestras = new[] { new ResourceSample(0, 1024 * 1024, 0) };
        var agg = ResourceAggregator.Aggregate(muestras, 1);

        Assert.Null(agg.CpuPorcentaje);
        Assert.Equal(0, agg.MuestrasCpu);
        Assert.NotNull(agg.MemoriaMb);
    }

    [Fact]
    public void IntervalosAnomalos_SeIgnoran()
    {
        var muestras = new[]
        {
            new ResourceSample(0, 1024 * 1024, 0),
            new ResourceSample(500, 1024 * 1024, 0),   // ΔWall = 0 → ignorado
            new ResourceSample(300, 1024 * 1024, 1000) // ΔCpu < 0 → ignorado
        };

        Assert.Null(ResourceAggregator.Aggregate(muestras, 1).CpuPorcentaje);
    }
}
