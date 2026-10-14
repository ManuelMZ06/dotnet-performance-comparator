using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class ConsultaResultadosModeloTests
{
    // Ids de catálogo estables.
    private const int Cpu = 1, Ram = 2, Tr = 3, Lat = 4, Thr = 5, Tam = 6;

    private sealed class FakeServicio : IConsultaResultadosService
    {
        public IReadOnlyList<EvaluacionResumenConsulta> Lista { get; set; } = [];
        public Func<int, EvaluacionDetalleConsulta?>? DetallePorId { get; set; }
        public Exception? LanzarEnListado { get; set; }
        public Dictionary<int, TaskCompletionSource<EvaluacionDetalleConsulta?>>? Compuertas { get; set; }

        public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken ct = default)
        {
            if (LanzarEnListado is not null) throw LanzarEnListado;
            return Task.FromResult(Lista);
        }

        public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
        {
            if (Compuertas is not null && Compuertas.TryGetValue(id, out var tcs)) return tcs.Task;
            return Task.FromResult(DetallePorId?.Invoke(id));
        }
    }

    private static ConsultaResultadosModelo Crear(FakeServicio fake) => new(fake, NullLogger.Instance);

    private static ResultadoMetricaConsulta R(int id, string nombre, string unidad, decimal valor) => new(id, nombre, unidad, valor);
    private static MetricaSeleccionadaConsulta M(int id, string nombre, string unidad) => new(id, nombre, unidad);

    private static EvaluacionResumenConsulta Resumen(int id, string nombre = "Eval", bool tiene = true) =>
        new(id, nombre, "App", "Carga", "Finalizada", 2, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), tiene);

    private static EvaluacionDetalleConsulta Detalle(
        int id, IReadOnlyList<EjecucionConsulta>? ejecuciones = null, IReadOnlyList<MetricaSeleccionadaConsulta>? metricas = null) =>
        new(id, $"Eval {id}", "App", ".NET 8 → .NET 10 (Exitosa)", "Carga", "Finalizada", 2, "/", null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), metricas ?? [], ejecuciones ?? []);

    private static EjecucionConsulta Ejecucion(
        string version, int numero, string estado,
        IReadOnlyList<ResultadoMetricaConsulta>? resultados = null, IReadOnlyList<NivelEstresConsulta>? niveles = null) =>
        new(version, numero, estado, DateTime.UtcNow, DateTime.UtcNow, [], resultados ?? [], niveles ?? []);

    // =============================================================== CU-05 (listado / detalle)

    [Fact]
    public async Task CargarListado_Vacio_MarcaListadoVacio()
    {
        var modelo = Crear(new FakeServicio { Lista = [] });
        await modelo.CargarListadoAsync();
        Assert.Empty(modelo.Evaluaciones);
        Assert.True(modelo.ListadoVacio);
    }

    [Fact]
    public async Task CargarListado_ConEvaluaciones_LasExpone()
    {
        var modelo = Crear(new FakeServicio { Lista = [Resumen(1, "A"), Resumen(2, "B", tiene: false)] });
        await modelo.CargarListadoAsync();
        Assert.Equal(2, modelo.Evaluaciones.Count);
        Assert.False(modelo.ListadoVacio);
        Assert.False(modelo.Evaluaciones.Single(e => e.Id == 2).TieneResultados);
    }

    [Fact]
    public async Task CargarListado_Error_MuestraMensajeYListaVacia()
    {
        var modelo = Crear(new FakeServicio { LanzarEnListado = new InvalidOperationException("db") });
        await modelo.CargarListadoAsync();
        Assert.Empty(modelo.Evaluaciones);
        Assert.Equal(OperationMessageKind.Error, modelo.MensajeTipo);
        Assert.False(string.IsNullOrWhiteSpace(modelo.Mensaje));
    }

    [Fact]
    public async Task Seleccionar_CargaDetalleCorrecto()
    {
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", [R(Cpu, "CPU", "%", 0.25m)])]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(7);
        Assert.Equal(7, modelo.SeleccionId);
        Assert.Equal(7, modelo.Detalle!.Id);
        Assert.False(modelo.EvaluacionNoEncontrada);
    }

    [Fact]
    public async Task Seleccionar_Inexistente_MarcaNoEncontrada()
    {
        var modelo = Crear(new FakeServicio { DetallePorId = _ => null });
        await modelo.SeleccionarAsync(99);
        Assert.True(modelo.EvaluacionNoEncontrada);
        Assert.Null(modelo.Detalle);
    }

    [Fact]
    public async Task Seleccionar_SinResultados_TieneResultadosFalse()
    {
        var fake = new FakeServicio { DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Fallida")]) };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(3);
        Assert.False(modelo.Detalle!.TieneResultados);
        Assert.Empty(modelo.Detalle.Ejecuciones.Single().Resultados);
    }

    [Fact]
    public async Task CambioDeSeleccion_UltimaGana_IgnoraRespuestaTardia()
    {
        var fake = new FakeServicio
        {
            Compuertas = new() { [1] = new(), [2] = new() }
        };
        var modelo = Crear(fake);
        var t1 = modelo.SeleccionarAsync(1);
        var t2 = modelo.SeleccionarAsync(2);
        fake.Compuertas![2].SetResult(Detalle(2));
        await t2;
        fake.Compuertas[1].SetResult(Detalle(1));
        await t1;
        Assert.Equal(2, modelo.SeleccionId);
        Assert.Equal(2, modelo.Detalle!.Id);
    }

    [Fact]
    public async Task CerrarDetalle_LimpiaEstado()
    {
        var modelo = Crear(new FakeServicio { DetallePorId = id => Detalle(id) });
        await modelo.SeleccionarAsync(1);
        modelo.CerrarDetalle();
        Assert.Null(modelo.SeleccionId);
        Assert.Null(modelo.Detalle);
    }

    // =============================================================== formato

    [Theory]
    [InlineData(0.0647, "0.0647")]
    [InlineData(85.2373, "85.2373")]
    [InlineData(48.07, "48.07")]
    [InlineData(100, "100")]
    public void FormatoValor_HastaCuatroDecimalesSinRelleno(decimal valor, string esperado)
    {
        Assert.Equal(esperado, ConsultaResultadosModelo.FormatoValor(valor));
    }

    [Fact]
    public void ValorConUnidad_UsaUnidadDelDto()
    {
        Assert.Equal("0.25 %", ConsultaResultadosModelo.ValorConUnidad(R(Cpu, "CPU", "%", 0.25m)));
        Assert.Equal("82.5", ConsultaResultadosModelo.ValorConUnidad(R(99, "X", "", 82.5m)));
    }

    // =============================================================== CU-06 (filtro por métrica)

    private static ConsultaResultadosModelo CargaSeisMetricas(int id = 1)
    {
        var resultados = new[]
        {
            R(Cpu, "CPU", "%", 0.25m), R(Ram, "Memoria RAM", "MB", 82.5m), R(Tr, "Tiempo de respuesta", "ms", 0.54m),
            R(Lat, "Latencia", "ms", 0.88m), R(Thr, "Throughput", "req/s", 47.6m), R(Tam, "Tamaño de la aplicación", "MB", 7.98m)
        };
        var metricas = new[]
        {
            M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB"), M(Tr, "Tiempo de respuesta", "ms"),
            M(Lat, "Latencia", "ms"), M(Thr, "Throughput", "req/s"), M(Tam, "Tamaño de la aplicación", "MB")
        };
        var fake = new FakeServicio
        {
            DetallePorId = cid => Detalle(cid,
                [Ejecucion(".NET 8", 1, "Completada", resultados), Ejecucion(".NET 10", 1, "Completada", resultados)],
                metricas)
        };
        return Crear(fake);
    }

    [Fact]
    public async Task Filtro_EstadoInicial_SinFiltroTodosVisibles()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);

        Assert.Null(modelo.MetricaFiltroId);
        Assert.False(modelo.FiltroActivo);
        var eje = modelo.Detalle!.Ejecuciones.First();
        Assert.Equal(6, modelo.ResultadosVisibles(eje).Count); // todos
    }

    [Fact]
    public async Task Filtro_MetricasDisponibles_ProvienenDeLaEvaluacion()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);
        Assert.Equal(modelo.Detalle!.MetricasSeleccionadas, modelo.MetricasDisponibles);
        Assert.Equal(6, modelo.MetricasDisponibles.Count);
    }

    [Fact]
    public async Task Filtro_SeleccionarCpu_SoloCpuVisible()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);

        var eje = modelo.Detalle!.Ejecuciones.First();
        var visibles = modelo.ResultadosVisibles(eje);
        Assert.True(modelo.FiltroActivo);
        Assert.Equal("CPU", Assert.Single(visibles).Metrica);
    }

    [Fact]
    public async Task Filtro_CambiarCpuARam_ActualizaResultados()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);
        modelo.AplicarFiltroMetrica(Ram);

        var eje = modelo.Detalle!.Ejecuciones.First();
        Assert.Equal("Memoria RAM", Assert.Single(modelo.ResultadosVisibles(eje)).Metrica);
    }

    [Fact]
    public async Task Filtro_Limpiar_RestauraTodos()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);
        modelo.QuitarFiltroMetrica();

        Assert.False(modelo.FiltroActivo);
        Assert.Equal(6, modelo.ResultadosVisibles(modelo.Detalle!.Ejecuciones.First()).Count);
    }

    [Fact]
    public async Task Filtro_EjecucionSinLaMetrica_SigueVisibleSinCeroFicticio()
    {
        // Config con CPU y RAM; la ejecución cancelada no tiene resultados.
        var metricas = new[] { M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB") };
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id,
                [Ejecucion(".NET 8", 1, "Completada", [R(Cpu, "CPU", "%", 0.3m), R(Ram, "Memoria RAM", "MB", 80m)]),
                 Ejecucion(".NET 10", 1, "Cancelada")],
                metricas)
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);

        var cancelada = modelo.Detalle!.Ejecuciones.Single(e => e.Estado == "Cancelada");
        Assert.Empty(modelo.ResultadosVisibles(cancelada)); // ausencia: lista vacía, ningún 0 inventado
        Assert.Contains(modelo.Detalle.Ejecuciones, e => e.Estado == "Cancelada"); // la ejecución sigue visible
        Assert.False(modelo.FiltroSinCoincidencias); // hay coincidencias (la .NET 8 sí tiene CPU)
    }

    [Fact]
    public async Task Filtro_Estres_FiltraResumenYNivelesPorLaMismaMetrica()
    {
        var niveles = new[]
        {
            new NivelEstresConsulta(1, 5, "Completado", 100, 0, null, [R(Cpu, "CPU", "%", 0.5m), R(Ram, "Memoria RAM", "MB", 77m)]),
            new NivelEstresConsulta(2, 10, "Completado", 200, 0, null, [R(Cpu, "CPU", "%", 0.2m), R(Ram, "Memoria RAM", "MB", 79m)])
        };
        var resumen = new[] { R(Cpu, "CPU", "%", 0.2m), R(Ram, "Memoria RAM", "MB", 79m) };
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", resumen, niveles)], [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);

        var eje = modelo.Detalle!.Ejecuciones.Single();
        Assert.Equal("CPU", Assert.Single(modelo.ResultadosVisibles(eje)).Metrica); // resumen filtrado
        Assert.Equal(2, eje.Niveles.Count); // todos los niveles visibles
        Assert.All(eje.Niveles, n => Assert.Equal("CPU", Assert.Single(modelo.ResultadosVisibles(n)).Metrica));
    }

    [Fact]
    public async Task Filtro_NivelSinLaMetrica_PermaneceVisible()
    {
        var niveles = new[]
        {
            new NivelEstresConsulta(1, 5, "Completado", 100, 0, null, [R(Cpu, "CPU", "%", 0.5m)]),
            new NivelEstresConsulta(2, 10, "Saturado", 0, 80, "0 exitosas", []) // sin métricas
        };
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "CompletadaConSaturacion", niveles: niveles)], [M(Cpu, "CPU", "%")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);

        var eje = modelo.Detalle!.Ejecuciones.Single();
        Assert.Equal(2, eje.Niveles.Count); // ningún nivel se elimina
        Assert.Empty(modelo.ResultadosVisibles(eje.Niveles[1])); // nivel sin CPU → vacío (se mostrará como ausencia)
    }

    [Fact]
    public async Task Filtro_TamanoEnEstres_NoApareceArtificialmentePorNivel()
    {
        var niveles = new[] { new NivelEstresConsulta(1, 5, "Completado", 100, 0, null, [R(Cpu, "CPU", "%", 0.5m)]) };
        var resumen = new[] { R(Cpu, "CPU", "%", 0.2m), R(Tam, "Tamaño de la aplicación", "MB", 7.98m) };
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", resumen, niveles)], [M(Cpu, "CPU", "%"), M(Tam, "Tamaño de la aplicación", "MB")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Tam);

        var eje = modelo.Detalle!.Ejecuciones.Single();
        Assert.Equal("Tamaño de la aplicación", Assert.Single(modelo.ResultadosVisibles(eje)).Metrica); // resumen sí
        Assert.Empty(modelo.ResultadosVisibles(eje.Niveles[0])); // nivel: nunca Tamaño
    }

    [Fact]
    public async Task Filtro_SaturadaSinResumen_ConservaNivelesFiltrados()
    {
        var niveles = new[]
        {
            new NivelEstresConsulta(1, 5, "Completado", 100, 0, null, [R(Ram, "Memoria RAM", "MB", 77m)]),
            new NivelEstresConsulta(2, 10, "Saturado", 0, 80, "0 exitosas", [R(Ram, "Memoria RAM", "MB", 85m)])
        };
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "CompletadaConSaturacion", niveles: niveles)], [M(Ram, "Memoria RAM", "MB")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Ram);

        var eje = modelo.Detalle!.Ejecuciones.Single();
        Assert.Empty(modelo.ResultadosVisibles(eje)); // sin resumen
        Assert.Equal(2, eje.Niveles.Count);
        Assert.All(eje.Niveles, n => Assert.Equal("Memoria RAM", Assert.Single(modelo.ResultadosVisibles(n)).Metrica));
        Assert.False(modelo.FiltroSinCoincidencias); // hay coincidencias por nivel
    }

    [Fact]
    public async Task Filtro_MetricaSinCoincidencias_SeDistingueDeSinResultados()
    {
        // Evaluación con CPU y RAM configuradas, pero solo hay resultados de RAM; se filtra CPU.
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", [R(Ram, "Memoria RAM", "MB", 80m)])], [M(Cpu, "CPU", "%"), M(Ram, "Memoria RAM", "MB")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);

        Assert.True(modelo.Detalle!.TieneResultados);   // la evaluación SÍ tiene resultados (RAM)
        Assert.True(modelo.FiltroSinCoincidencias);     // pero el filtro CPU no coincide
    }

    [Fact]
    public async Task Filtro_CambioDeEvaluacion_LimpiaElFiltro()
    {
        var fake = new FakeServicio
        {
            DetallePorId = id => Detalle(id, [Ejecucion(".NET 8", 1, "Completada", [R(Cpu, "CPU", "%", 0.3m)])], [M(Cpu, "CPU", "%")])
        };
        var modelo = Crear(fake);
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);
        await modelo.SeleccionarAsync(2);

        Assert.Null(modelo.MetricaFiltroId);
    }

    [Fact]
    public async Task Filtro_CerrarDetalle_LimpiaElFiltro()
    {
        var modelo = CargaSeisMetricas();
        await modelo.SeleccionarAsync(1);
        modelo.AplicarFiltroMetrica(Cpu);
        modelo.CerrarDetalle();

        Assert.Null(modelo.MetricaFiltroId);
    }

    [Fact]
    public async Task Filtro_RespuestaTardia_NoRestauraFiltroDeEvaluacionAnterior()
    {
        var fake = new FakeServicio { Compuertas = new() { [1] = new(), [2] = new() } };
        fake.DetallePorId = id => id == 2
            ? Detalle(2, [Ejecucion(".NET 8", 1, "Completada", [R(Lat, "Latencia", "ms", 1m)])], [M(Lat, "Latencia", "ms")])
            : Detalle(1, [Ejecucion(".NET 8", 1, "Completada", [R(Cpu, "CPU", "%", 0.3m)])], [M(Cpu, "CPU", "%")]);
        var modelo = Crear(fake);

        var t1 = modelo.SeleccionarAsync(1);  // pendiente
        var t2 = modelo.SeleccionarAsync(2);  // última selección (filtro reiniciado)
        fake.Compuertas![2].SetResult(fake.DetallePorId(2));
        await t2;
        modelo.AplicarFiltroMetrica(Lat);      // filtro válido para la evaluación 2
        fake.Compuertas[1].SetResult(fake.DetallePorId(1)); // respuesta tardía de la 1
        await t1;

        Assert.Equal(2, modelo.Detalle!.Id);
        Assert.Equal(Lat, modelo.MetricaFiltroId);      // la respuesta tardía no cambió el filtro
        Assert.Equal("Latencia", modelo.MetricasDisponibles.Single().Nombre); // métricas de la evaluación actual
    }

    // =============================================================== CU-07 (navegación a M-07)

    [Fact]
    public void UrlComparacion_SinFiltro_NoInventaMetricaId()
    {
        Assert.Equal("/comparacion/42", ConsultaResultadosModelo.UrlComparacion(42, null));
    }

    [Fact]
    public void UrlComparacion_ConFiltro_PropagaMetricaId()
    {
        Assert.Equal("/comparacion/42?metricaId=1", ConsultaResultadosModelo.UrlComparacion(42, 1));
    }

    [Fact]
    public void UrlComparacion_UsaElConfiguracionIdIndicado()
    {
        Assert.StartsWith("/comparacion/7", ConsultaResultadosModelo.UrlComparacion(7, 5));
    }
}
