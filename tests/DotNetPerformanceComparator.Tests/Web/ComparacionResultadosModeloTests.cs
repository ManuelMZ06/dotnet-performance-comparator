using DotNetPerformanceComparator.Application.Evaluations.Comparison;
using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

/// <summary>
/// Pruebas del presentador de M-07 (CU-07, C-10.3). Se enfoca en estado/formato/presentación: usa un
/// <see cref="IConsultaResultadosService"/> falso y el motor real (puro) de Application, sin repetir la
/// aritmética ya cubierta por <c>ComparacionResultadosServiceTests</c>.
/// </summary>
public class ComparacionResultadosModeloTests
{
    private const int Cpu = 1, Ram = 2, Lat = 4, Thr = 5, Tam = 6;
    private const string Net8 = VersionesDotNet.Net8;
    private const string Net10 = VersionesDotNet.Net10;

    private sealed class FakeConsulta : IConsultaResultadosService
    {
        public Func<int, EvaluacionDetalleConsulta?>? DetallePorId { get; set; }
        public Exception? Lanzar { get; set; }

        public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<EvaluacionResumenConsulta>>([]);

        public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
        {
            if (Lanzar is not null) throw Lanzar;
            return Task.FromResult(DetallePorId?.Invoke(id));
        }
    }

    private static ComparacionResultadosModelo Crear(FakeConsulta fake) =>
        new(fake, new ComparacionResultadosService(), NullLogger.Instance);

    // ------------------------------------------------------------------ helpers de datos

    private static MetricaSeleccionadaConsulta M(int id, string nombre, string unidad) => new(id, nombre, unidad);
    private static ResultadoMetricaConsulta R(int id, decimal valor, string nombre = "M", string unidad = "u") => new(id, nombre, unidad, valor);

    private static NivelEstresConsulta Niv(int usuarios, string estado, params ResultadoMetricaConsulta[] resultados) =>
        new(usuarios, usuarios, estado, 10, 0, null, resultados);

    private static EjecucionConsulta Eje(string version, int rep, string estado,
        ResultadoMetricaConsulta[]? resultados = null, NivelEstresConsulta[]? niveles = null) =>
        new(version, rep, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], niveles ?? []);

    private static EvaluacionDetalleConsulta Detalle(string tipo, MetricaSeleccionadaConsulta[] metricas, params EjecucionConsulta[] ejecuciones) =>
        new(7, "Eval", "App", ".NET 8 → .NET 10 (Exitosa)", tipo, "Finalizada", ejecuciones.Length, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), metricas, ejecuciones);

    private static FakeConsulta ConDetalle(EvaluacionDetalleConsulta detalle) => new() { DetallePorId = _ => detalle };

    /// <summary>Carga simple con CPU: .NET 8 = 0.50, .NET 10 = 0.40 (Mejora, -20 %).</summary>
    private static EvaluacionDetalleConsulta CargaCpu() => Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")],
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)]),
        Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.40m)]));

    // ================================================================= estado

    [Fact]
    public async Task Carga_EvaluacionExistente_ExponeDetalleYResumen()
    {
        var modelo = Crear(ConDetalle(CargaCpu()));
        await modelo.CargarAsync(7);

        Assert.False(modelo.Cargando);
        Assert.False(modelo.EvaluacionNoEncontrada);
        Assert.NotNull(modelo.Detalle);
        Assert.NotNull(modelo.Resumen);
        Assert.False(modelo.EsEstres);
    }

    [Fact]
    public async Task Inexistente_MarcaNoEncontrada()
    {
        var modelo = Crear(new FakeConsulta { DetallePorId = _ => null });
        await modelo.CargarAsync(99);

        Assert.True(modelo.EvaluacionNoEncontrada);
        Assert.Null(modelo.Resumen);
    }

    [Fact]
    public async Task Error_MuestraMensajeError()
    {
        var modelo = Crear(new FakeConsulta { Lanzar = new InvalidOperationException("db") });
        await modelo.CargarAsync(1);

        Assert.Equal(OperationMessageKind.Error, modelo.MensajeTipo);
        Assert.False(string.IsNullOrWhiteSpace(modelo.Mensaje));
    }

    [Fact]
    public async Task ComparacionNormal_EsComparable()
    {
        var modelo = Crear(ConDetalle(CargaCpu()));
        await modelo.CargarAsync(7);
        Assert.True(modelo.Resumen!.EsComparable);
    }

    [Fact]
    public async Task ComparacionParcial_UnaNoComparableNoInvalida()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Lat, "Latencia", "ms")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m), R(Lat, 1.0m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.40m)])); // sin Latencia
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        Assert.True(modelo.Resumen!.EsComparable);
        Assert.Contains(modelo.Resumen.Metricas, m => m.MetricaId == Lat && m.Interpretacion == InterpretacionComparacion.NoComparable);
    }

    [Fact]
    public async Task NingunaComparable_EsComparableFalse()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)])); // solo .NET 8
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);
        Assert.False(modelo.Resumen!.EsComparable);
    }

    // ================================================================= filtro / enfoque

    [Fact]
    public async Task FiltroNull_MuestraTodas()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Ram, 70m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Ram, 68m)]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        Assert.False(modelo.FiltroActivo);
        Assert.Equal(2, modelo.MetricasVisibles(modelo.Resumen!).Count);
    }

    [Fact]
    public async Task FiltroValido_MuestraSoloEsaMetrica()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Ram, 70m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Ram, 68m)]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7, Cpu);

        Assert.True(modelo.FiltroActivo);
        Assert.Equal(Cpu, Assert.Single(modelo.MetricasVisibles(modelo.Resumen!)).MetricaId);
    }

    [Fact]
    public async Task MetricaIdInvalido_SeRestableceATodas()
    {
        var modelo = Crear(ConDetalle(CargaCpu()));
        await modelo.CargarAsync(7, 999); // 999 no pertenece a la evaluación

        Assert.Null(modelo.MetricaFiltroId);
        Assert.False(modelo.FiltroActivo);
    }

    // ================================================================= formato (estático)

    [Fact]
    public void Porcentaje_Null_EsNoCalculable()
    {
        Assert.Equal("No calculable", ComparacionResultadosModelo.Porcentaje(null));
    }

    [Fact]
    public void Porcentaje_ConservaSigno()
    {
        Assert.Equal("-20 %", ComparacionResultadosModelo.Porcentaje(-20m));
        Assert.Equal("+15.5 %", ComparacionResultadosModelo.Porcentaje(15.5m));
    }

    [Fact]
    public void Diferencia_ConservaSignoMatematico()
    {
        var mejora = new ComparacionMetrica(Cpu, "CPU", "%", OrientacionMetrica.MenorEsMejor,
            0.50m, 0.40m, -0.10m, -20m, InterpretacionComparacion.Mejora, 1);
        Assert.Equal("-0.1 %", ComparacionResultadosModelo.Diferencia(mejora)); // Mejora pero diferencia negativa

        var empeora = new ComparacionMetrica(Thr, "Throughput", "req/s", OrientacionMetrica.MayorEsMejor,
            40m, 55m, 15m, 37.5m, InterpretacionComparacion.Mejora, 1);
        Assert.Equal("+15 req/s", ComparacionResultadosModelo.Diferencia(empeora)); // Mejora con diferencia positiva
    }

    [Fact]
    public void Diferencia_NoComparable_EsGuion()
    {
        var nc = new ComparacionMetrica(Cpu, "CPU", "%", OrientacionMetrica.MenorEsMejor,
            null, null, null, null, InterpretacionComparacion.NoComparable, 0);
        Assert.Equal("—", ComparacionResultadosModelo.Diferencia(nc));
        Assert.Equal("—", ComparacionResultadosModelo.Valor(null, "%"));
    }

    [Theory]
    [InlineData(InterpretacionComparacion.Mejora, "Mejora")]
    [InlineData(InterpretacionComparacion.Empeora, "Empeora")]
    [InlineData(InterpretacionComparacion.Igual, "Igual")]
    [InlineData(InterpretacionComparacion.NoComparable, "No comparable")]
    public void InterpretacionTexto_SiempreTextoVisible(InterpretacionComparacion i, string esperado)
    {
        Assert.Equal(esperado, ComparacionResultadosModelo.InterpretacionTexto(i));
    }

    [Fact]
    public void ParesTexto_PorMetrica()
    {
        var dos = new ComparacionMetrica(Cpu, "CPU", "%", OrientacionMetrica.MenorEsMejor, 1m, 1m, 0m, 0m, InterpretacionComparacion.Igual, 2);
        Assert.Equal("2 pares comparables", ComparacionResultadosModelo.ParesTexto(dos));

        var cero = new ComparacionMetrica(Lat, "Latencia", "ms", OrientacionMetrica.MenorEsMejor, null, null, null, null, InterpretacionComparacion.NoComparable, 0);
        Assert.Equal("sin pares comparables", ComparacionResultadosModelo.ParesTexto(cero));
    }

    [Fact]
    public void ParesTexto_Tamano_EsUnaComparacionConstante()
    {
        var tam = new ComparacionMetrica(Tam, "Tamaño de la aplicación", "MB", OrientacionMetrica.MenorEsMejor,
            8m, 7m, -1m, -12.5m, InterpretacionComparacion.Mejora, 1);
        var texto = ComparacionResultadosModelo.ParesTexto(tam);
        Assert.Contains("constante", texto);
        Assert.DoesNotContain("muestras", texto);
    }

    // ================================================================= conclusión

    [Fact]
    public async Task Conclusion_ConteosCorrectos()
    {
        // CPU mejora (0.5→0.4), Throughput empeora (55→40), Latencia igual (1→1).
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Cpu, "CPU", "%"), M(Thr, "Throughput", "req/s"), M(Lat, "Latencia", "ms")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.5m), R(Thr, 55m), R(Lat, 1m)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m), R(Thr, 40m), R(Lat, 1m)]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        var c = modelo.Conclusion();
        Assert.Equal(3, c.Total);
        Assert.Equal(1, c.Mejoras);
        Assert.Equal(1, c.Empeoramientos);
        Assert.Equal(1, c.Iguales);
        Assert.Equal(0, c.NoComparables);
    }

    [Fact]
    public async Task Conclusion_NoDeclaraGanadorGlobal()
    {
        var modelo = Crear(ConDetalle(CargaCpu()));
        await modelo.CargarAsync(7);
        var texto = modelo.Conclusion().Texto.ToLowerInvariant();

        Assert.DoesNotContain("ganador", texto);
        Assert.DoesNotContain("superior", texto);
        Assert.DoesNotContain("mejor versión", texto);
    }

    // ================================================================= Estrés

    private static EvaluacionDetalleConsulta EstresConMaximo() => Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
        Eje(Net8, 1, EstadosEjecucion.Completada, [R(Cpu, 0.60m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.6m))]),
        Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.50m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.5m))]));

    [Fact]
    public async Task Estres_ResumenComparable()
    {
        var modelo = Crear(ConDetalle(EstresConMaximo()));
        await modelo.CargarAsync(7);

        Assert.True(modelo.EsEstres);
        Assert.True(modelo.Resumen!.EsComparable);
        Assert.NotEmpty(modelo.Estres!.Niveles);
    }

    [Fact]
    public async Task Estres_ResumenNoDisponiblePorSaturacion()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(20, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(20, EstadosNivelEstres.Saturado)]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        Assert.False(modelo.Resumen!.EsComparable);
        Assert.Contains(modelo.Estres!.Niveles, n => n.UsuariosObjetivo == 10 && n.NivelComparable); // nivel anterior sí
    }

    [Fact]
    public async Task Estres_NivelParcialmenteComparable_VisibleConUnPar()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.5m))]),
            Eje(Net10, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.4m))]),
            Eje(Net8, 2, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(40, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 2, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(40, EstadosNivelEstres.Completado, R(Cpu, 0.45m))]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        var nivel40 = modelo.Estres!.Niveles.Single(n => n.UsuariosObjetivo == 40);
        Assert.True(nivel40.NivelComparable);
        Assert.Equal(1, nivel40.Metricas.Single(m => m.MetricaId == Cpu).ParesUtilizados);
    }

    [Fact]
    public async Task Estres_NivelNoComparable_Visible()
    {
        var detalle = Detalle(TiposPruebaConocidos.Estres, [M(Cpu, "CPU", "%")],
            Eje(Net8, 1, EstadosEjecucion.CompletadaConSaturacion, niveles: [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.5m)), Niv(20, EstadosNivelEstres.Saturado)]),
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Cpu, 0.4m)], [Niv(10, EstadosNivelEstres.Completado, R(Cpu, 0.4m)), Niv(20, EstadosNivelEstres.Completado, R(Cpu, 0.5m))]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        var nivel20 = modelo.Estres!.Niveles.Single(n => n.UsuariosObjetivo == 20);
        Assert.False(nivel20.NivelComparable); // .NET 8 saturó en 20
    }

    // ================================================================= contexto de saturación (texto)

    [Fact]
    public void ContextoSaturacionTexto_Consistente_MinIgualMax()
    {
        var ctx = new ContextoSaturacionVersion(20, true, 30, 30);
        Assert.Equal("Saturación observada: 30 usuarios", ComparacionResultadosModelo.ContextoSaturacionTexto(ctx));
    }

    [Fact]
    public void ContextoSaturacionTexto_Variable_MuestraRango()
    {
        var ctx = new ContextoSaturacionVersion(30, true, 30, 40);
        Assert.Equal("Saturación observada: entre 30 y 40 usuarios", ComparacionResultadosModelo.ContextoSaturacionTexto(ctx));
    }

    [Fact]
    public void ContextoSaturacionTexto_SinSaturacion()
    {
        var ctx = new ContextoSaturacionVersion(50, false, null, null);
        Assert.Equal("No se observó saturación en los niveles ejecutados.", ComparacionResultadosModelo.ContextoSaturacionTexto(ctx));
    }

    [Fact]
    public void ContextoMaximoTexto_RotuladoComoObservado()
    {
        var ctx = new ContextoSaturacionVersion(30, true, 40, 40);
        var texto = ComparacionResultadosModelo.ContextoMaximoTexto(ctx);
        Assert.Contains("observado", texto);
        Assert.Contains("30", texto);
    }

    // ================================================================= notas / inconsistencias

    [Fact]
    public async Task TamanoInconsistente_ExponeNotaDeFormaSegura()
    {
        var detalle = Detalle(TiposPruebaConocidos.Carga, [M(Tam, "Tamaño de la aplicación", "MB")],
            Eje(Net8, 1, EstadosEjecucion.Completada, [R(Tam, 8m)]),
            Eje(Net8, 2, EstadosEjecucion.Completada, [R(Tam, 9m)]), // inconsistente
            Eje(Net10, 1, EstadosEjecucion.Completada, [R(Tam, 7m)]),
            Eje(Net10, 2, EstadosEjecucion.Completada, [R(Tam, 7m)]));
        var modelo = Crear(ConDetalle(detalle));
        await modelo.CargarAsync(7);

        var tam = modelo.Resumen!.Metricas.Single(m => m.MetricaId == Tam);
        Assert.Equal(InterpretacionComparacion.NoComparable, tam.Interpretacion);
        Assert.NotNull(tam.Nota); // disponible para mostrarse de forma secundaria
    }
}
