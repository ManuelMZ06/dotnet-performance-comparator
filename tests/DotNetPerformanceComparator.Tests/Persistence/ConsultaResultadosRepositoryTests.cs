using DotNetPerformanceComparator.Application.Evaluations.Results;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class ConsultaResultadosRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();
    private readonly ConsultaResultadosRepository _repo;
    private int _proyectoSeq;

    public ConsultaResultadosRepositoryTests() => _repo = new ConsultaResultadosRepository(new Factory(_database));

    public void Dispose() => _database.Dispose();

    private sealed class Factory(SqliteInMemoryDatabase db) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => db.CreateContext();
    }

    // ---------------------------------------------------------------- seeding

    /// <summary>Crea una evaluación (ConfiguracionEvaluacion) con su proyecto y migración, y devuelve su Id.</summary>
    private int CrearEvaluacion(
        string nombre, int tipoId = 1, string estado = "Finalizada", DateTime? fecha = null,
        int numeroEjecuciones = 2, int[]? metricaIds = null)
    {
        using var context = _database.CreateContext();
        var proyecto = new ProyectoDotNet
        {
            Nombre = $"App {++_proyectoSeq}", RutaOriginal = $@"C:\App{_proyectoSeq}\App.csproj",
            VersionBase = ".NET 8", EstadoValidacion = "Valido", FechaRegistro = DateTime.UtcNow
        };
        proyecto.Migraciones.Add(new Migracion { VersionOrigen = ".NET 8", VersionDestino = ".NET 10", Estado = "Exitosa", FechaInicio = DateTime.UtcNow });
        context.ProyectosDotNet.Add(proyecto);
        context.SaveChanges();

        var config = new ConfiguracionEvaluacion
        {
            ProyectoId = proyecto.Id, MigracionId = proyecto.Migraciones.First().Id, TipoPruebaId = tipoId,
            Nombre = nombre, NumeroEjecuciones = numeroEjecuciones, RutaObjetivo = "/",
            Estado = estado, FechaCreacion = fecha ?? DateTime.UtcNow
        };
        foreach (var m in metricaIds ?? [1, 2, 3])
        {
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = m });
        }
        context.ConfiguracionesEvaluacion.Add(config);
        context.SaveChanges();
        return config.Id;
    }

    private int AgregarEjecucion(
        int configId, string version, int numero, string estado,
        (int MetricaId, decimal Valor)[]? resultados = null, (string Clave, string Valor)[]? condiciones = null)
    {
        using var context = _database.CreateContext();
        var ejecucion = new Ejecucion
        {
            ConfiguracionId = configId, VersionDotNet = version, NumeroEjecucion = numero,
            FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow, Estado = estado
        };
        foreach (var (metricaId, valor) in resultados ?? [])
        {
            ejecucion.Resultados.Add(new ResultadoMetrica { MetricaId = metricaId, Valor = valor });
        }
        foreach (var (clave, valor) in condiciones ?? [])
        {
            ejecucion.Condiciones.Add(new CondicionEjecucion { Clave = clave, Valor = valor });
        }
        context.Ejecuciones.Add(ejecucion);
        context.SaveChanges();
        return ejecucion.Id;
    }

    private void AgregarNivel(
        int ejecucionId, int numeroNivel, int usuarios, string estado,
        int exitosas, int fallidas, (int MetricaId, decimal Valor)[]? resultados = null, string? detalle = null)
    {
        using var context = _database.CreateContext();
        var nivel = new NivelEstres
        {
            EjecucionId = ejecucionId, NumeroNivel = numeroNivel, UsuariosObjetivo = usuarios, Estado = estado,
            FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow,
            SolicitudesExitosas = exitosas, SolicitudesFallidas = fallidas, Detalle = detalle
        };
        foreach (var (metricaId, valor) in resultados ?? [])
        {
            nivel.Resultados.Add(new ResultadoMetricaNivelEstres { MetricaId = metricaId, Valor = valor });
        }
        context.NivelesEstres.Add(nivel);
        context.SaveChanges();
    }

    private void AgregarParametros(int configId, (string Clave, string Valor)[] parametros)
    {
        using var context = _database.CreateContext();
        foreach (var (clave, valor) in parametros)
        {
            context.Add(new ParametroEvaluacion { ConfiguracionId = configId, Clave = clave, Valor = valor });
        }
        context.SaveChanges();
    }

    // ---------------------------------------------------------------- listado

    [Fact]
    public async Task Listado_Vacio_DevuelveListaVacia()
    {
        Assert.Empty(await _repo.ListarAsync());
    }

    [Fact]
    public async Task Listado_VariasEvaluaciones_LasDevuelveTodas()
    {
        CrearEvaluacion("A");
        CrearEvaluacion("B");
        CrearEvaluacion("C");

        var lista = await _repo.ListarAsync();
        Assert.Equal(3, lista.Count);
        Assert.Contains(lista, e => e.Nombre == "B");
    }

    [Fact]
    public async Task Listado_OrdenDeterminista_MasRecientesPrimero()
    {
        var baseUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        CrearEvaluacion("Antigua", fecha: baseUtc);
        CrearEvaluacion("Media", fecha: baseUtc.AddDays(5));
        CrearEvaluacion("Reciente", fecha: baseUtc.AddDays(10));

        var nombres = (await _repo.ListarAsync()).Select(e => e.Nombre).ToArray();
        Assert.Equal(new[] { "Reciente", "Media", "Antigua" }, nombres);
    }

    [Fact]
    public async Task Listado_TieneResultados_ReflejaLoPersistido()
    {
        var conResultados = CrearEvaluacion("Con resultados");
        AgregarEjecucion(conResultados, ".NET 8", 1, EstadosEjecucion.Completada, [(1, 0.25m)]);
        CrearEvaluacion("Sin resultados");

        var lista = await _repo.ListarAsync();
        Assert.True(lista.Single(e => e.Nombre == "Con resultados").TieneResultados);
        Assert.False(lista.Single(e => e.Nombre == "Sin resultados").TieneResultados);
    }

    // ---------------------------------------------------------------- detalle

    [Fact]
    public async Task Detalle_EvaluacionInexistente_DevuelveNull()
    {
        Assert.Null(await _repo.ObtenerDetalleAsync(99999));
    }

    [Fact]
    public async Task Detalle_ExistenteSinResultados_TieneResultadosFalse()
    {
        var id = CrearEvaluacion("Configurada sin ejecutar", estado: "Configurada");

        var detalle = await _repo.ObtenerDetalleAsync(id);
        Assert.NotNull(detalle);
        Assert.False(detalle!.TieneResultados);
        Assert.Empty(detalle.Ejecuciones);
        Assert.Equal(3, detalle.MetricasSeleccionadas.Count);
    }

    [Fact]
    public async Task Detalle_EjecucionesNet8YNet10_ConResultadosPersistidos()
    {
        var id = CrearEvaluacion("Carga", metricaIds: [1, 2, 3]);
        AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.Completada,
            [(1, 0.25m), (2, 82.5m), (3, 0.54m)], [(ClavesCondicionEjecucion.SolicitudesExitosas, "700")]);
        AgregarEjecucion(id, ".NET 10", 1, EstadosEjecucion.Completada,
            [(1, 0.21m), (2, 80.1m), (3, 0.49m)]);

        var detalle = await _repo.ObtenerDetalleAsync(id);
        Assert.NotNull(detalle);
        Assert.True(detalle!.TieneResultados);
        Assert.Equal(2, detalle.Ejecuciones.Count);
        Assert.Contains(detalle.Ejecuciones, e => e.VersionDotNet == ".NET 8");
        Assert.Contains(detalle.Ejecuciones, e => e.VersionDotNet == ".NET 10");

        var net8 = detalle.Ejecuciones.Single(e => e.VersionDotNet == ".NET 8");
        var cpu = net8.Resultados.Single(r => r.Metrica == "CPU");
        Assert.Equal(0.25m, cpu.Valor);
        Assert.Equal("%", cpu.Unidad); // unidad del catálogo
        Assert.Contains(net8.Condiciones, c => c.Clave == ClavesCondicionEjecucion.SolicitudesExitosas && c.Valor == "700");
    }

    [Fact]
    public async Task Detalle_NoInventaMetricasAusentes()
    {
        var id = CrearEvaluacion("Solo CPU", metricaIds: [1]);
        AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.Completada, [(1, 0.3m)]);

        var detalle = await _repo.ObtenerDetalleAsync(id);
        var ejecucion = Assert.Single(detalle!.Ejecuciones);
        var resultado = Assert.Single(ejecucion.Resultados);
        Assert.Equal("CPU", resultado.Metrica); // no aparecen RAM/Latencia/etc.
    }

    [Fact]
    public async Task Detalle_EjecucionFallidaOCancelada_SinResultadosNiCeros()
    {
        var id = CrearEvaluacion("Con fallo");
        AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.Completada, [(1, 0.25m)]);
        AgregarEjecucion(id, ".NET 10", 1, EstadosEjecucion.Fallida); // sin resultados

        var detalle = await _repo.ObtenerDetalleAsync(id);
        var fallida = detalle!.Ejecuciones.Single(e => e.Estado == EstadosEjecucion.Fallida);
        Assert.Empty(fallida.Resultados); // sin valores ficticios ni ceros
        var completada = detalle.Ejecuciones.Single(e => e.Estado == EstadosEjecucion.Completada);
        Assert.NotEmpty(completada.Resultados); // la anterior se conserva
    }

    // ---------------------------------------------------------------- estrés

    [Fact]
    public async Task Detalle_Estres_DevuelveResultadosPorNivel()
    {
        var id = CrearEvaluacion("Estrés", tipoId: 2, metricaIds: [1, 2, 3, 4, 5]);
        var ejec = AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.Completada, [(1, 0.1m), (2, 80m), (3, 0.5m), (4, 1.1m), (5, 60m)]);
        AgregarNivel(ejec, 1, 5, EstadosNivelEstres.Completado, exitosas: 100, fallidas: 0, resultados: [(1, 0.5m), (2, 77m)]);
        AgregarNivel(ejec, 2, 10, EstadosNivelEstres.Completado, exitosas: 200, fallidas: 0, resultados: [(1, 0.2m), (2, 79m)]);

        var detalle = await _repo.ObtenerDetalleAsync(id);
        var ejecucion = Assert.Single(detalle!.Ejecuciones);
        Assert.Equal(2, ejecucion.Niveles.Count);
        Assert.Equal(new[] { 1, 2 }, ejecucion.Niveles.Select(n => n.NumeroNivel).ToArray()); // orden por NumeroNivel
        var nivel1 = ejecucion.Niveles[0];
        Assert.Equal(5, nivel1.UsuariosObjetivo);
        Assert.Equal(0.5m, nivel1.Resultados.Single(r => r.Metrica == "CPU").Valor);
    }

    [Fact]
    public async Task Detalle_EstresSaturado_ConservaNivelesYNoInventaResumen()
    {
        var id = CrearEvaluacion("Estrés saturado", tipoId: 2, metricaIds: [1, 2]);
        // Ejecucion CompletadaConSaturacion: niveles previos completados + nivel saturado, SIN ResultadoMetrica resumen.
        var ejec = AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.CompletadaConSaturacion); // sin resultados resumen
        AgregarNivel(ejec, 1, 5, EstadosNivelEstres.Completado, exitosas: 120, fallidas: 0, resultados: [(1, 0.3m), (2, 78m)]);
        AgregarNivel(ejec, 2, 10, EstadosNivelEstres.Saturado, exitosas: 0, fallidas: 80, resultados: [(1, 0.9m)], detalle: "0 solicitudes exitosas");

        var detalle = await _repo.ObtenerDetalleAsync(id);
        var ejecucion = Assert.Single(detalle!.Ejecuciones);
        Assert.Equal(EstadosEjecucion.CompletadaConSaturacion, ejecucion.Estado);
        Assert.Empty(ejecucion.Resultados); // no se inventa un resumen para la ejecución saturada
        Assert.Equal(2, ejecucion.Niveles.Count); // niveles previos conservados

        var saturado = ejecucion.Niveles.Single(n => n.Estado == EstadosNivelEstres.Saturado);
        Assert.Equal(0, saturado.SolicitudesExitosas);
        Assert.Equal(80, saturado.SolicitudesFallidas);
        Assert.Contains("0 solicitudes", saturado.Detalle);
        Assert.Equal(0.9m, saturado.Resultados.Single(r => r.Metrica == "CPU").Valor); // CPU/RAM confiables persistidas
        Assert.True(detalle.TieneResultados); // hay resultados por nivel aunque no haya resumen
    }

    [Fact]
    public async Task TieneResultados_SoloResultadosPorNivel_EsTrueEnListadoYDetalle()
    {
        // Estrés con ResultadoMetricaNivelEstres pero SIN ResultadoMetrica resumen: debe considerarse con
        // resultados consultables, y con la MISMA semántica en listado y detalle (consistencia).
        var id = CrearEvaluacion("Estrés solo nivel", tipoId: 2, metricaIds: [1, 2]);
        var ejec = AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.CompletadaConSaturacion); // sin resumen
        AgregarNivel(ejec, 1, 5, EstadosNivelEstres.Completado, exitosas: 100, fallidas: 0, resultados: [(1, 0.4m)]);

        var enListado = (await _repo.ListarAsync()).Single(e => e.Id == id);
        var detalle = await _repo.ObtenerDetalleAsync(id);

        Assert.True(enListado.TieneResultados);                 // listado
        Assert.True(detalle!.TieneResultados);                  // detalle (misma semántica)
        Assert.Empty(detalle.Ejecuciones.Single().Resultados);  // no hay resumen de ejecución
    }

    [Fact]
    public async Task TieneResultados_NivelSinResultados_NoCuentaComoResultado()
    {
        // Un NivelEstres existente pero sin ningún ResultadoMetricaNivelEstres NO vuelve true por sí solo;
        // tampoco las condiciones de ejecución. (Caso F.)
        var id = CrearEvaluacion("Nivel sin métricas", tipoId: 2, metricaIds: [1]);
        var ejec = AgregarEjecucion(id, ".NET 8", 1, EstadosEjecucion.Fallida,
            condiciones: [(ClavesCondicionEjecucion.SolicitudesFallidas, "0")]); // solo condiciones, sin resultados
        AgregarNivel(ejec, 1, 5, EstadosNivelEstres.Cancelado, exitosas: 0, fallidas: 0); // nivel sin resultados

        var enListado = (await _repo.ListarAsync()).Single(e => e.Id == id);
        var detalle = await _repo.ObtenerDetalleAsync(id);

        Assert.False(enListado.TieneResultados);
        Assert.False(detalle!.TieneResultados);
    }

    // ---------------------------------------------------------------- parámetros (CU-09, read model)

    [Fact]
    public async Task Detalle_ParametrosCarga_SeProyectanTipados()
    {
        var id = CrearEvaluacion("Carga", tipoId: 1);
        AgregarParametros(id,
        [
            (ClavesParametroCarga.UsuariosVirtuales, "50"), (ClavesParametroCarga.DuracionSegundos, "30"),
            (ClavesParametroCarga.WarmUpSegundos, "5"), (ClavesParametroCarga.RampUpSegundos, "10"),
            (ClavesParametroCarga.IntervaloSolicitudesMs, "100")
        ]);

        var p = Assert.IsType<ParametrosCargaConsulta>((await _repo.ObtenerDetalleAsync(id))!.Parametros);
        Assert.Equal(50, p.UsuariosVirtuales);
        Assert.Equal(30, p.DuracionSegundos);
        Assert.Equal(5, p.WarmUpSegundos);
        Assert.Equal(10, p.RampUpSegundos);
        Assert.Equal(100, p.IntervaloSolicitudesMs);
    }

    [Fact]
    public async Task Detalle_ParametrosEstres_SeProyectanTipados()
    {
        var id = CrearEvaluacion("Estrés", tipoId: 2);
        AgregarParametros(id,
        [
            (ClavesParametroEstres.UsuariosIniciales, "5"), (ClavesParametroEstres.IncrementoUsuarios, "5"),
            (ClavesParametroEstres.UsuariosMaximos, "20"), (ClavesParametroEstres.DuracionNivelSegundos, "15"),
            (ClavesParametroEstres.WarmUpSegundos, "3"), (ClavesParametroEstres.IntervaloSolicitudesMs, "50")
        ]);

        var p = Assert.IsType<ParametrosEstresConsulta>((await _repo.ObtenerDetalleAsync(id))!.Parametros);
        Assert.Equal(5, p.UsuariosIniciales);
        Assert.Equal(5, p.IncrementoUsuarios);
        Assert.Equal(20, p.UsuariosMaximos);
        Assert.Equal(15, p.DuracionNivelSegundos);
        Assert.Equal(3, p.WarmUpSegundos);
        Assert.Equal(50, p.IntervaloSolicitudesMs);
    }

    [Fact]
    public async Task Detalle_ParametrosConcurrencia_SeProyectanTipados()
    {
        var id = CrearEvaluacion("Concurrencia", tipoId: 3);
        AgregarParametros(id,
        [
            (ClavesParametroConcurrencia.UsuariosConcurrentes, "10"), (ClavesParametroConcurrencia.Rondas, "4"),
            (ClavesParametroConcurrencia.WarmUpSegundos, "2"), (ClavesParametroConcurrencia.IntervaloRondasMs, "500")
        ]);

        var p = Assert.IsType<ParametrosConcurrenciaConsulta>((await _repo.ObtenerDetalleAsync(id))!.Parametros);
        Assert.Equal(10, p.UsuariosConcurrentes);
        Assert.Equal(4, p.Rondas);
        Assert.Equal(2, p.WarmUpSegundos);
        Assert.Equal(500, p.IntervaloRondasMs);
    }

    [Fact]
    public async Task Detalle_SinParametros_NoRompeElDetalle_YSonNull()
    {
        var id = CrearEvaluacion("Sin parámetros", tipoId: 1);
        var detalle = await _repo.ObtenerDetalleAsync(id);

        Assert.NotNull(detalle); // la extensión del read model no rompe el detalle existente
        var p = Assert.IsType<ParametrosCargaConsulta>(detalle!.Parametros);
        Assert.Null(p.UsuariosVirtuales); // ausentes → null, NO 0 ficticio
        Assert.Null(p.DuracionSegundos);
    }

    [Fact]
    public async Task Detalle_ParametroAusente_EsNull_NoCero()
    {
        var id = CrearEvaluacion("Carga parcial", tipoId: 1);
        AgregarParametros(id,
        [
            (ClavesParametroCarga.UsuariosVirtuales, "40"),
            // DuracionSegundos ausente a propósito
            (ClavesParametroCarga.WarmUpSegundos, "0") // 0 real persistido
        ]);

        var p = Assert.IsType<ParametrosCargaConsulta>((await _repo.ObtenerDetalleAsync(id))!.Parametros);
        Assert.Equal(40, p.UsuariosVirtuales);
        Assert.Null(p.DuracionSegundos);   // ausente → null
        Assert.Equal(0, p.WarmUpSegundos); // 0 real sí se conserva
    }

    [Fact]
    public async Task Detalle_ParametroNoNumerico_EsNull_NoCero()
    {
        var id = CrearEvaluacion("Carga inconsistente", tipoId: 1);
        AgregarParametros(id, [(ClavesParametroCarga.UsuariosVirtuales, "cincuenta")]); // no parseable

        var p = Assert.IsType<ParametrosCargaConsulta>((await _repo.ObtenerDetalleAsync(id))!.Parametros);
        Assert.Null(p.UsuariosVirtuales); // dato no interpretable → null, no 0
    }
}
