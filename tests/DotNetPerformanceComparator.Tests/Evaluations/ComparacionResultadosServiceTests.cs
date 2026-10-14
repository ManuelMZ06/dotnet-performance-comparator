using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Tests.Evaluations;

/// <summary>
/// Pruebas del motor comparativo CU-07 (C-10.2). Determinista y puro: opera sobre
/// <see cref="EvaluacionDetalleConsulta"/> sin EF ni UI. .NET 8 es base; pares completos por métrica y
/// repetición; igualdad exacta; base cero → porcentaje null; Tamaño constante por versión; Estrés por
/// niveles equivalentes completados; saturación como contexto.
/// </summary>
public class ComparacionResultadosServiceTests
{
    private const int Cpu = 1, Ram = 2, Tr = 3, Lat = 4, Thr = 5, Tam = 6;
    private const string Net8 = VersionesDotNet.Net8;
    private const string Net10 = VersionesDotNet.Net10;

    private static readonly ComparacionResultadosService Servicio = new();

    // ------------------------------------------------------------------ helpers de construcción

    private static MetricaSeleccionadaConsulta M(int id, string nombre = "M", string unidad = "u") => new(id, nombre, unidad);

    private static ResultadoMetricaConsulta R(int id, decimal valor, string nombre = "M", string unidad = "u") => new(id, nombre, unidad, valor);

    private static NivelEstresConsulta Niv(int usuarios, string estado, params ResultadoMetricaConsulta[] resultados) =>
        new(usuarios, usuarios, estado, 10, 0, null, resultados);

    private static EjecucionConsulta Eje(
        string version, int rep, string estado,
        ResultadoMetricaConsulta[]? resultados = null, NivelEstresConsulta[]? niveles = null) =>
        new(version, rep, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], niveles ?? []);

    private static EvaluacionDetalleConsulta Detalle(
        string tipoPrueba, MetricaSeleccionadaConsulta[] metricas, params EjecucionConsulta[] ejecuciones) =>
        new(1, "Eval", "App", ".NET 8 → .NET 10 (Exitosa)", tipoPrueba, "Finalizada", ejecuciones.Length,
            "/", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), metricas, ejecuciones);

    private static ComparacionMetrica Metrica(ComparacionResumen c, int metricaId) => c.Metricas.Single(m => m.MetricaId == metricaId);

    private static readonly MetricaSeleccionadaConsulta[] SoloCpu = [M(Cpu, "CPU", "%")];

    // ================================================================= Carga / Concurrencia

    [Fact]
    public void UnaRepeticionCompleta_PromediaElUnicoPar()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.10m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(1, cpu.ParesUtilizados);
        Assert.Equal(0.20m, cpu.PromedioNet8);
        Assert.Equal(0.10m, cpu.PromedioNet10);
        Assert.Equal(-0.10m, cpu.DiferenciaAbsoluta);
        Assert.Equal(-50m, cpu.DiferenciaPorcentual);
        Assert.Equal(InterpretacionComparacion.Mejora, cpu.Interpretacion);
    }

    [Fact]
    public void DosRepeticionesCompletas_PromediaAmbosPares()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.10m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Cpu, 0.40m)]),
            Eje(Net10, 2, EstadosEjecucion.Completada, [R(Cpu, 0.30m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(2, cpu.ParesUtilizados);
        Assert.Equal(0.30m, cpu.PromedioNet8);
        Assert.Equal(0.20m, cpu.PromedioNet10);
    }

    [Fact]
    public void TresRepeticionesCompletas_PromediaLosTresPares()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.30m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.10m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Cpu, 0.30m)]),
            Eje(Net10, 2, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net8, 3, EstadosEjecucion.Completada, [R(Cpu, 0.30m)]),
            Eje(Net10, 3, EstadosEjecucion.Completada, [R(Cpu, 0.30m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(3, cpu.ParesUtilizados);
        Assert.Equal(0.30m, cpu.PromedioNet8);
        Assert.Equal(0.20m, cpu.PromedioNet10);
    }

    [Fact]
    public void ParIncompleto_SoloCuentaElParCompletoEnAmbasVersiones()
    {
        // R2 de .NET 10 ausente: R2 no debe entrar en NINGÚN promedio.
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.10m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Cpu, 0.80m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(1, cpu.ParesUtilizados);
        Assert.Equal(0.20m, cpu.PromedioNet8); // NO (0.20+0.80)/2
        Assert.Equal(0.10m, cpu.PromedioNet10);
    }

    [Fact]
    public void MetricaSinPares_EsNoComparable()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)])); // sin .NET 10

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(InterpretacionComparacion.NoComparable, cpu.Interpretacion);
        Assert.Equal(0, cpu.ParesUtilizados);
        Assert.Null(cpu.PromedioNet8);
        Assert.Null(cpu.PromedioNet10);
        Assert.Null(cpu.DiferenciaAbsoluta);
    }

    [Fact]
    public void BaseNet8Cero_PorcentajeNullPeroComparable()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Thr, "Throughput", "req/s")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Thr, 0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Thr, 5m)]));

        var thr = Metrica(Servicio.CompararResumen(detalle), Thr);
        Assert.Null(thr.DiferenciaPorcentual);
        Assert.Equal(5m, thr.DiferenciaAbsoluta);
        Assert.Equal(InterpretacionComparacion.Mejora, thr.Interpretacion); // mayor es mejor
        Assert.NotEqual(InterpretacionComparacion.NoComparable, thr.Interpretacion);
    }

    [Fact]
    public void AmbosCero_IgualYPorcentajeNull()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(InterpretacionComparacion.Igual, cpu.Interpretacion);
        Assert.Equal(0m, cpu.DiferenciaAbsoluta);
        Assert.Null(cpu.DiferenciaPorcentual);
    }

    [Fact]
    public void ValoresExactamenteIguales_Igual()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.1234m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.1234m)]));

        var cpu = Metrica(Servicio.CompararResumen(detalle), Cpu);
        Assert.Equal(InterpretacionComparacion.Igual, cpu.Interpretacion);
        Assert.Equal(0m, cpu.DiferenciaPorcentual);
    }

    [Fact]
    public void CpuNet10Menor_Mejora()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.40m)]));
        Assert.Equal(InterpretacionComparacion.Mejora, Metrica(Servicio.CompararResumen(detalle), Cpu).Interpretacion);
    }

    [Fact]
    public void CpuNet10Mayor_Empeora()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.40m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)]));
        Assert.Equal(InterpretacionComparacion.Empeora, Metrica(Servicio.CompararResumen(detalle), Cpu).Interpretacion);
    }

    [Fact]
    public void ThroughputNet10Mayor_Mejora()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Thr, "Throughput", "req/s")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Thr, 40m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Thr, 55m)]));
        Assert.Equal(InterpretacionComparacion.Mejora, Metrica(Servicio.CompararResumen(detalle), Thr).Interpretacion);
    }

    [Fact]
    public void ThroughputNet10Menor_Empeora()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Thr, "Throughput", "req/s")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Thr, 55m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Thr, 40m)]));
        Assert.Equal(InterpretacionComparacion.Empeora, Metrica(Servicio.CompararResumen(detalle), Thr).Interpretacion);
    }

    [Fact]
    public void UnaMetricaNoComparable_NoInvalidaLasDemas()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Lat, "Latencia", "ms")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m), R(Lat, 1.0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.10m)])); // sin Latencia en .NET 10

        var resumen = Servicio.CompararResumen(detalle);
        Assert.True(resumen.EsComparable);
        Assert.Equal(InterpretacionComparacion.Mejora, Metrica(resumen, Cpu).Interpretacion);
        Assert.Equal(InterpretacionComparacion.NoComparable, Metrica(resumen, Lat).Interpretacion);
    }

    [Fact]
    public void NingunaMetricaComparable_EsComparableFalse()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)])); // solo .NET 8

        var resumen = Servicio.CompararResumen(detalle);
        Assert.False(resumen.EsComparable);
        Assert.NotNull(resumen.NotaNoComparable);
    }

    [Fact]
    public void EjecucionFallida_NoEntraComoFuente()
    {
        // La .NET 10 está Fallida; aunque (defensivamente) tuviera un valor, no debe usarse.
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net10, 1, EstadosEjecucion.Fallida, [R(Cpu, 0.10m)]));

        Assert.Equal(InterpretacionComparacion.NoComparable, Metrica(Servicio.CompararResumen(detalle), Cpu).Interpretacion);
    }

    [Fact]
    public void EjecucionCancelada_NoEntraComoFuente()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.20m)]),
            Eje(Net10, 1, EstadosEjecucion.Cancelada, [R(Cpu, 0.10m)]));

        Assert.Equal(InterpretacionComparacion.NoComparable, Metrica(Servicio.CompararResumen(detalle), Cpu).Interpretacion);
    }

    // ================================================================= Tamaño (constante por versión)

    [Fact]
    public void Tamano_UnValorPorVersion_Compara()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 8.0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 7.0m)]));

        var tam = Metrica(Servicio.CompararResumen(detalle), Tam);
        Assert.Equal(1, tam.ParesUtilizados);
        Assert.Equal(8.0m, tam.PromedioNet8);
        Assert.Equal(7.0m, tam.PromedioNet10);
        Assert.Equal(InterpretacionComparacion.Mejora, tam.Interpretacion); // menor huella
    }

    [Fact]
    public void Tamano_TresRepeticionesMismoValor_EsConstanteNoTresMediciones()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 7.97m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 6.50m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Tam, 7.97m)]),
            Eje(Net10, 2, EstadosEjecucion.Completada, [R(Tam, 6.50m)]),
            Eje(Net8, 3, EstadosEjecucion.Completada, [R(Tam, 7.97m)]),
            Eje(Net10, 3, EstadosEjecucion.Completada, [R(Tam, 6.50m)]));

        var tam = Metrica(Servicio.CompararResumen(detalle), Tam);
        Assert.Equal(1, tam.ParesUtilizados); // UNA comparación, no 3
        Assert.Equal(7.97m, tam.PromedioNet8);
        Assert.Equal(6.50m, tam.PromedioNet10);
    }

    [Fact]
    public void Tamano_MenorEnNet10_Mejora()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 10m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 9m)]));
        Assert.Equal(InterpretacionComparacion.Mejora, Metrica(Servicio.CompararResumen(detalle), Tam).Interpretacion);
    }

    [Fact]
    public void Tamano_InconsistenteEnUnaVersion_NoComparableSinPromediar()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 8.0m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Tam, 9.0m)]), // valores distintos en .NET 8
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 7.0m)]),
            Eje(Net10, 2, EstadosEjecucion.Completada, [R(Tam, 7.0m)]));

        var tam = Metrica(Servicio.CompararResumen(detalle), Tam);
        Assert.Equal(InterpretacionComparacion.NoComparable, tam.Interpretacion);
        Assert.Equal(0, tam.ParesUtilizados);
        Assert.Null(tam.PromedioNet8);
        Assert.NotNull(tam.Nota);
    }

    // ================================================================= Estrés — resumen

    [Fact]
    public void Estres_AmbasAlcanzanMaximo_ResumenComparable()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.60m)],
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.50m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.60m))]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)],
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.40m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.50m))]));

        var resumen = Servicio.CompararEstres(detalle).Resumen;
        Assert.True(resumen.EsComparable);
        Assert.Equal(InterpretacionComparacion.Mejora, Metrica(resumen, Cpu).Interpretacion);
    }

    [Fact]
    public void Estres_UnaSatura_EsaRepeticionNoAportaParDeResumen()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.60m)],
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.50m))]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.40m)), Niv(20, EstadosNivelEstres.Saturado)]));

        var resumen = Servicio.CompararEstres(detalle).Resumen;
        Assert.Equal(InterpretacionComparacion.NoComparable, Metrica(resumen, Cpu).Interpretacion);
    }

    [Fact]
    public void Estres_SinParesDeResumen_ResumenNoDisponible()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, SoloCpu,
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.50m)), Niv(20, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.40m)), Niv(20, EstadosNivelEstres.Saturado)]));

        var resumen = Servicio.CompararEstres(detalle).Resumen;
        Assert.False(resumen.EsComparable);
        Assert.NotNull(resumen.NotaNoComparable);
    }

    // ================================================================= Estrés — por nivel

    /// <summary>
    /// Escenario base por nivel: .NET 8 completa 10/20 y satura en 30; .NET 10 completa 10/20/30 y satura en 40.
    /// </summary>
    private static EvaluacionDetalleConsulta EstresNiveles(params MetricaSeleccionadaConsulta[] metricas) =>
        Detalle(TiposPruebaConocidos.Estres, metricas,
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
            [
                Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.50m), R(Ram, 70m)),
                Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.60m), R(Ram, 75m)),
                Niv(30, EstadosNivelEstres.Saturado)
            ]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
            [
                Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.40m), R(Ram, 68m)),
                Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.50m), R(Ram, 72m)),
                Niv(30, EstadosNivelEstres.Completado, R(Cpu, 0.70m), R(Ram, 80m)),
                Niv(40, EstadosNivelEstres.Saturado)
            ]));

    private static ComparacionNivelEstres NivelDe(ComparacionEstres c, int usuarios) => c.Niveles.Single(n => n.UsuariosObjetivo == usuarios);

    [Fact]
    public void Estres_MismosNivelesCompletados_SonComparables()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%")));
        var nivel10 = NivelDe(c, 10);
        Assert.True(nivel10.NivelComparable);
        Assert.Equal(InterpretacionComparacion.Mejora, nivel10.Metricas.Single(m => m.MetricaId == Cpu).Interpretacion);
    }

    [Fact]
    public void Estres_EmparejamientoPorUsuariosObjetivo()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%")));
        var nivel20 = NivelDe(c, 20);
        var cpu = nivel20.Metricas.Single(m => m.MetricaId == Cpu);
        Assert.Equal(0.60m, cpu.PromedioNet8); // nivel 20 de .NET 8
        Assert.Equal(0.50m, cpu.PromedioNet10); // nivel 20 de .NET 10 (no otro nivel)
    }

    [Fact]
    public void Estres_NivelSoloEnUnaVersion_NoComparable()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%")));
        var nivel40 = NivelDe(c, 40); // solo en .NET 10
        Assert.False(nivel40.NivelComparable);
        Assert.Contains(".NET 10", nivel40.NotaNivel);
    }

    [Fact]
    public void Estres_NivelSaturadoVsCompletado_NoComparable()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%")));
        var nivel30 = NivelDe(c, 30); // .NET 8 Saturado, .NET 10 Completado
        Assert.False(nivel30.NivelComparable);
    }

    [Fact]
    public void Estres_AmbosSaturadosEnNivel_NoEsComparacionNormal()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(20, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(20, EstadosNivelEstres.Saturado)]));

        var c = Servicio.CompararEstres(detalle);
        Assert.False(NivelDe(c, 20).NivelComparable);
        Assert.True(NivelDe(c, 10).NivelComparable); // nivel anterior sí
    }

    [Fact]
    public void Estres_NivelesAnterioresCompletados_SiguenComparandose()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%")));
        Assert.True(NivelDe(c, 10).NivelComparable);
        Assert.True(NivelDe(c, 20).NivelComparable);
    }

    [Fact]
    public void Estres_MetricaAusenteEnNivel_SoloEsaMetricaNoComparable()
    {
        // Nivel 10 completado en ambas, pero .NET 10 no tiene RAM en el nivel 10.
        var detalle = Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m), R(Ram, 70m))]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m))]));

        var nivel10 = NivelDe(Servicio.CompararEstres(detalle), 10);
        Assert.True(nivel10.NivelComparable);
        Assert.Equal(InterpretacionComparacion.Mejora, nivel10.Metricas.Single(m => m.MetricaId == Cpu).Interpretacion);
        Assert.Equal(InterpretacionComparacion.NoComparable, nivel10.Metricas.Single(m => m.MetricaId == Ram).Interpretacion);
    }

    [Fact]
    public void Estres_TamanoExcluidoPorNivel()
    {
        var c = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%"), M(Tam, "Tamaño de la aplicación", "MB")));
        Assert.All(c.Niveles, n => Assert.DoesNotContain(n.Metricas, m => m.MetricaId == Tam));
    }

    // ================================================================= Estrés — contexto de saturación

    [Fact]
    public void Estres_ContextoMaxUsuariosCompletados_Correcto()
    {
        var contexto = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%"))).Contexto;
        Assert.Equal(20, contexto.Net8.MaxUsuariosCompletados);
        Assert.Equal(30, contexto.Net10.MaxUsuariosCompletados);
    }

    [Fact]
    public void Estres_NivelSaturacionObservado_Correcto()
    {
        var contexto = Servicio.CompararEstres(EstresNiveles(M(Cpu, "CPU", "%"))).Contexto;
        Assert.True(contexto.Net8.Saturo);
        Assert.Equal(30, contexto.Net8.NivelSaturacionMinObservado); // única saturación → min == max
        Assert.Equal(30, contexto.Net8.NivelSaturacionMaxObservado);
        Assert.True(contexto.Net10.Saturo);
        Assert.Equal(40, contexto.Net10.NivelSaturacionMinObservado);
        Assert.Equal(40, contexto.Net10.NivelSaturacionMaxObservado);
    }

    [Fact]
    public void Estres_SaturacionDistintaPorVersion_RepresentadaCorrectamente()
    {
        // .NET 8 satura a 40; .NET 10 satura a 60.
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(30, EstadosNivelEstres.Completado, R(Cpu, 0.6m)), Niv(40, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(30, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.7m)), Niv(50, EstadosNivelEstres.Completado, R(Cpu, 0.8m)), Niv(60, EstadosNivelEstres.Saturado)]));

        var contexto = Servicio.CompararEstres(detalle).Contexto;
        Assert.Equal(40, contexto.Net8.NivelSaturacionMinObservado);
        Assert.Equal(40, contexto.Net8.NivelSaturacionMaxObservado);
        Assert.Equal(30, contexto.Net8.MaxUsuariosCompletados);
        Assert.Equal(60, contexto.Net10.NivelSaturacionMinObservado);
        Assert.Equal(60, contexto.Net10.NivelSaturacionMaxObservado);
        Assert.Equal(50, contexto.Net10.MaxUsuariosCompletados);
    }

    // ================================================================= C-10.2.1 — auditoría por nivel

    [Fact] // A: una repetición comparable y otra saturada → el nivel sigue comparable con 1 par.
    public void Estres_NivelParcialmenteValido_SigueComparableConUnPar()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.50m)), Niv(50, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.40m)), Niv(50, EstadosNivelEstres.Saturado)]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Saturado, R(Cpu, 9.99m))]), // R2 .NET 8 satura en 40 (valor NO debe usarse)
            Eje(Net10, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.45m))]));

        var nivel40 = NivelDe(Servicio.CompararEstres(detalle), 40);
        var cpu = nivel40.Metricas.Single(m => m.MetricaId == Cpu);
        Assert.True(nivel40.NivelComparable);
        Assert.Equal(1, cpu.ParesUtilizados);           // solo R1
        Assert.Equal(0.50m, cpu.PromedioNet8);          // NO promedia con 9.99 del nivel Saturado
        Assert.Equal(0.40m, cpu.PromedioNet10);
        Assert.Equal(InterpretacionComparacion.Mejora, cpu.Interpretacion);
    }

    [Fact] // B: métricas con distinto número de pares dentro del mismo nivel.
    public void Estres_MetricasConDistintoNumeroDePares_CadaUnaIndependiente()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB"), M(Lat, "Latencia", "ms")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.50m), R(Ram, 70m), R(Lat, 1.0m))]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.40m), R(Ram, 68m))]), // sin Latencia
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.60m))]), // sin RAM ni Latencia
            Eje(Net10, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.50m))])); // sin RAM ni Latencia

        var nivel20 = NivelDe(Servicio.CompararEstres(detalle), 20);
        Assert.True(nivel20.NivelComparable);
        Assert.Equal(2, nivel20.Metricas.Single(m => m.MetricaId == Cpu).ParesUtilizados); // R1 + R2
        var ram = nivel20.Metricas.Single(m => m.MetricaId == Ram);
        Assert.Equal(1, ram.ParesUtilizados); // solo R1
        Assert.NotEqual(InterpretacionComparacion.NoComparable, ram.Interpretacion);
        var lat = nivel20.Metricas.Single(m => m.MetricaId == Lat);
        Assert.Equal(0, lat.ParesUtilizados);
        Assert.Equal(InterpretacionComparacion.NoComparable, lat.Interpretacion);
    }

    [Fact] // C: cero pares válidos (ambas repeticiones saturadas en una versión) → nivel no comparable.
    public void Estres_NivelSinNingunPar_NoComparableNiUsaSaturado()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Saturado, R(Cpu, 9.99m))]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.40m))]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Saturado, R(Cpu, 9.99m))]),
            Eje(Net10, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.45m))]));

        var nivel40 = NivelDe(Servicio.CompararEstres(detalle), 40);
        Assert.False(nivel40.NivelComparable);
        Assert.Equal(InterpretacionComparacion.NoComparable, nivel40.Metricas.Single(m => m.MetricaId == Cpu).Interpretacion);
    }

    [Fact] // D: saturación distinta entre repeticiones → el contexto expone la variabilidad (min != max).
    public void Estres_SaturacionVariableEntreRepeticiones_ContextoNoOcultaVariabilidad()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.3m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(30, EstadosNivelEstres.Saturado)]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.3m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(30, EstadosNivelEstres.Completado, R(Cpu, 0.6m)), Niv(40, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.3m))]));

        var net8 = Servicio.CompararEstres(detalle).Contexto.Net8;
        Assert.True(net8.Saturo);
        Assert.Equal(30, net8.NivelSaturacionMinObservado); // R1 saturó en 30
        Assert.Equal(40, net8.NivelSaturacionMaxObservado); // R2 saturó en 40 → variabilidad visible
        Assert.Equal(30, net8.MaxUsuariosCompletados);      // máximo completado observado (R2 completó 30)
    }

    [Fact] // E: misma saturación en varias repeticiones → min == max (representación consistente).
    public void Estres_SaturacionConsistenteEntreRepeticiones_MinIgualMax()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, M(Cpu, "CPU", "%").ToSingle(),
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(30, EstadosNivelEstres.Saturado)]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles:
                [Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(30, EstadosNivelEstres.Saturado)]));

        var net8 = Servicio.CompararEstres(detalle).Contexto.Net8;
        Assert.True(net8.Saturo);
        Assert.Equal(30, net8.NivelSaturacionMinObservado);
        Assert.Equal(30, net8.NivelSaturacionMaxObservado); // consistente
    }
}

file static class MetricaArrayExtensions
{
    public static MetricaSeleccionadaConsulta[] ToSingle(this MetricaSeleccionadaConsulta m) => [m];
}
