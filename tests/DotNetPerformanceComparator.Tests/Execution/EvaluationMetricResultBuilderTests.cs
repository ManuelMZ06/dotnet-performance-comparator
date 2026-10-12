using DotNetPerformanceComparator.Application.Evaluations.Execution;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Tests.Execution;

public class EvaluationMetricResultBuilderTests
{
    // Catálogo con los Id seed reales.
    private static readonly Dictionary<string, int> Ids = new()
    {
        [NombresMetrica.Cpu] = 1,
        [NombresMetrica.MemoriaRam] = 2,
        [NombresMetrica.TiempoRespuesta] = 3,
        [NombresMetrica.Latencia] = 4,
        [NombresMetrica.Throughput] = 5,
        [NombresMetrica.TamanoAplicacion] = 6
    };

    private static List<Metrica> Metricas(params string[] nombres) =>
        nombres.Select(n => new Metrica { Id = Ids[n], Nombre = n, Unidad = "u" }).ToList();

    // Entradas válidas por defecto.
    private static MetricInputs Validas(double mean = 10.5, double p95 = 24.8, double rps = 47.6, long bytes = 16_000_000) =>
        new(CpuPorcentaje: 0.25m, MemoriaMb: 82.5m, TiempoRespuestaMedioMs: mean, LatenciaP95Ms: p95, ThroughputRps: rps, PublishedSizeBytes: bytes);

    [Fact]
    public void SoloCpu_UnResultado()
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(Metricas(NombresMetrica.Cpu), Validas());
        Assert.Null(faltante);
        var r = Assert.Single(res);
        Assert.Equal(1, r.MetricaId);
        Assert.Equal(0.25m, r.Valor);
    }

    [Fact]
    public void CpuYLatencia_DosResultados_NoIncluyeNoSeleccionadas()
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.Cpu, NombresMetrica.Latencia), Validas());

        Assert.Null(faltante);
        Assert.Equal(2, res.Count);
        Assert.Contains(res, x => x.MetricaId == Ids[NombresMetrica.Cpu]);
        Assert.Contains(res, x => x.MetricaId == Ids[NombresMetrica.Latencia]);
        Assert.DoesNotContain(res, x => x.MetricaId == Ids[NombresMetrica.Throughput]);
    }

    [Fact]
    public void SeisMetricas_SeisResultados()
    {
        var todas = Metricas(NombresMetrica.Cpu, NombresMetrica.MemoriaRam, NombresMetrica.TiempoRespuesta,
            NombresMetrica.Latencia, NombresMetrica.Throughput, NombresMetrica.TamanoAplicacion);

        var (res, faltante) = EvaluationMetricResultBuilder.Build(todas, Validas(bytes: 16 * 1024 * 1024));

        Assert.Null(faltante);
        Assert.Equal(6, res.Count);
    }

    [Fact]
    public void Latencia_UsaP95_NoMean()
    {
        var (res, _) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.TiempoRespuesta, NombresMetrica.Latencia), Validas(mean: 10.5, p95: 24.8));

        Assert.Equal(10.5m, res.Single(x => x.MetricaId == Ids[NombresMetrica.TiempoRespuesta]).Valor);
        Assert.Equal(24.8m, res.Single(x => x.MetricaId == Ids[NombresMetrica.Latencia]).Valor);
    }

    [Fact]
    public void Throughput_UsaRps()
    {
        var (res, _) = EvaluationMetricResultBuilder.Build(Metricas(NombresMetrica.Throughput), Validas(rps: 47.6));
        Assert.Equal(47.6m, Assert.Single(res).Valor);
    }

    [Fact]
    public void Tamano_ConvierteBytesAMb()
    {
        var (res, _) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.TamanoAplicacion), Validas(bytes: 15 * 1024 * 1024));
        Assert.Equal(15m, Assert.Single(res).Valor);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void P95NoFinito_MarcaFaltante(double p95)
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.Latencia), Validas(p95: p95));

        Assert.Equal(NombresMetrica.Latencia, faltante);
        Assert.Empty(res); // no se inventa 0/-1
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RpsNoFinito_MarcaFaltante(double rps)
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.Throughput), Validas(rps: rps));

        Assert.Equal(NombresMetrica.Throughput, faltante);
        Assert.Empty(res);
    }

    [Fact]
    public void ThroughputCero_MarcaFaltante()
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.Throughput), Validas(rps: 0));

        Assert.Equal(NombresMetrica.Throughput, faltante); // throughput debe ser > 0
        Assert.Empty(res);
    }

    [Fact]
    public void MeanNegativo_MarcaFaltante()
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.TiempoRespuesta), Validas(mean: -1));

        Assert.Equal(NombresMetrica.TiempoRespuesta, faltante);
        Assert.Empty(res);
    }

    [Fact]
    public void CpuNulo_MarcaFaltante()
    {
        var inputs = Validas() with { CpuPorcentaje = null };
        var (res, faltante) = EvaluationMetricResultBuilder.Build(Metricas(NombresMetrica.Cpu), inputs);

        Assert.Equal(NombresMetrica.Cpu, faltante);
        Assert.Empty(res);
    }

    [Fact]
    public void TamanoCero_MarcaFaltante()
    {
        var (res, faltante) = EvaluationMetricResultBuilder.Build(
            Metricas(NombresMetrica.TamanoAplicacion), Validas(bytes: 0));

        Assert.Equal(NombresMetrica.TamanoAplicacion, faltante);
        Assert.Empty(res);
    }
}
