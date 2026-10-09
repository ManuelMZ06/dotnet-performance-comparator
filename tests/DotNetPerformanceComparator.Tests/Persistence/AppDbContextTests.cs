using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public class AppDbContextTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static ProyectoDotNet NuevoProyecto() => new()
    {
        Nombre = "AppDemo",
        RutaOriginal = @"D:\Proyectos\AppDemo\AppDemo.csproj",
        VersionBase = "net8.0",
        EstadoValidacion = "Valido",
        FechaRegistro = new DateTime(2026, 10, 7, 10, 0, 0, DateTimeKind.Utc)
    };

    private static Migracion NuevaMigracion(ProyectoDotNet proyecto) => new()
    {
        Proyecto = proyecto,
        VersionOrigen = "net8.0",
        VersionDestino = "net10.0",
        Estado = "Exitosa",
        FechaInicio = DateTime.UtcNow
    };

    private int CrearEjecucion()
    {
        using var context = _database.CreateContext();
        var proyecto = NuevoProyecto();
        var configuracion = new ConfiguracionEvaluacion
        {
            Proyecto = proyecto,
            Migracion = NuevaMigracion(proyecto),
            Nombre = "Evaluación",
            NumeroEjecuciones = 1,
            RutaObjetivo = "/",
            FechaCreacion = DateTime.UtcNow,
            Estado = "Configurada"
        };
        var ejecucion = new Ejecucion
        {
            Configuracion = configuracion,
            VersionDotNet = "net8.0",
            NumeroEjecucion = 1,
            FechaInicio = DateTime.UtcNow,
            Estado = "Completada"
        };
        context.Ejecuciones.Add(ejecucion);
        context.SaveChanges();
        return ejecucion.Id;
    }

    [Fact]
    public void Modelo_SeCreaConTodasLasEntidadesPersistentes()
    {
        using var context = _database.CreateContext();

        var entidades = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        Assert.Equal(11, entidades.Count);
        Assert.DoesNotContain("Comparacion", entidades);
        Assert.DoesNotContain("Reporte", entidades);
    }

    [Fact]
    public void ProyectoDotNet_SeRegistraYRecupera()
    {
        using (var context = _database.CreateContext())
        {
            context.ProyectosDotNet.Add(NuevoProyecto());
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        var proyecto = Assert.Single(verificacion.ProyectosDotNet);
        Assert.True(proyecto.Id > 0);
        Assert.Equal("AppDemo", proyecto.Nombre);
        Assert.Equal(@"D:\Proyectos\AppDemo\AppDemo.csproj", proyecto.RutaOriginal);
    }

    [Fact]
    public void RelacionesPrincipales_SePersistenYRecuperan()
    {
        using (var context = _database.CreateContext())
        {
            var proyecto = NuevoProyecto();
            proyecto.Migraciones.Add(new Migracion
            {
                VersionOrigen = "net8.0",
                VersionDestino = "net10.0",
                Estado = "Completada",
                FechaInicio = DateTime.UtcNow
            });

            var configuracion = new ConfiguracionEvaluacion
            {
                Proyecto = proyecto,
                Migracion = proyecto.Migraciones.First(),
                Nombre = "Evaluación comparativa 01",
                NumeroEjecuciones = 3,
                RutaObjetivo = "/api/productos",
                Observaciones = "Condiciones equivalentes",
                FechaCreacion = DateTime.UtcNow,
                Estado = "Configurada"
            };
            configuracion.ConfiguracionesPrueba.Add(new ConfiguracionPrueba
            {
                TipoPruebaId = 1,
                Parametros = { new ParametroEvaluacion { TipoPruebaId = 1, Clave = "UsuariosVirtuales", Valor = "50" } }
            });
            configuracion.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = 3 });

            var ejecucion = new Ejecucion
            {
                VersionDotNet = "net8.0",
                NumeroEjecucion = 1,
                FechaInicio = DateTime.UtcNow,
                Estado = "Completada"
            };
            ejecucion.Condiciones.Add(new CondicionEjecucion { Clave = "SistemaOperativo", Valor = "Windows 11" });
            ejecucion.Resultados.Add(new ResultadoMetrica { MetricaId = 3, Valor = 12.3456789m });
            configuracion.Ejecuciones.Add(ejecucion);

            context.ConfiguracionesEvaluacion.Add(configuracion);
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        var cargado = verificacion.ProyectosDotNet
            .Include(p => p.Migraciones)
            .Include(p => p.ConfiguracionesEvaluacion).ThenInclude(c => c.ConfiguracionesPrueba).ThenInclude(cp => cp.TipoPrueba)
            .Include(p => p.ConfiguracionesEvaluacion).ThenInclude(c => c.ConfiguracionesPrueba).ThenInclude(cp => cp.Parametros)
            .Include(p => p.ConfiguracionesEvaluacion).ThenInclude(c => c.ConfiguracionesMetrica).ThenInclude(cm => cm.Metrica)
            .Include(p => p.ConfiguracionesEvaluacion).ThenInclude(c => c.Ejecuciones).ThenInclude(e => e.Condiciones)
            .Include(p => p.ConfiguracionesEvaluacion).ThenInclude(c => c.Ejecuciones).ThenInclude(e => e.Resultados).ThenInclude(r => r.Metrica)
            .AsSplitQuery()
            .Single();

        Assert.Single(cargado.Migraciones);
        var config = Assert.Single(cargado.ConfiguracionesEvaluacion);
        var prueba = Assert.Single(config.ConfiguracionesPrueba);
        Assert.Equal("Carga", prueba.TipoPrueba.Nombre);
        Assert.Equal("UsuariosVirtuales", Assert.Single(prueba.Parametros).Clave);
        Assert.Equal("Tiempo de respuesta", Assert.Single(config.ConfiguracionesMetrica).Metrica.Nombre);
        Assert.Equal("/api/productos", config.RutaObjetivo);
        var ejecucionCargada = Assert.Single(config.Ejecuciones);
        Assert.Equal("Windows 11", Assert.Single(ejecucionCargada.Condiciones).Valor);
        var resultado = Assert.Single(ejecucionCargada.Resultados);
        Assert.Equal(12.3456789m, resultado.Valor);
        Assert.Equal("ms", resultado.Metrica.Unidad);
    }

    [Fact]
    public void CatalogoTipoPrueba_ContieneLosTresTiposIniciales()
    {
        using var context = _database.CreateContext();

        var tipos = context.TiposPrueba.OrderBy(t => t.Id).Select(t => new { t.Id, t.Nombre }).ToList();

        Assert.Equal(
            new[] { new { Id = 1, Nombre = "Carga" }, new { Id = 2, Nombre = "Estrés" }, new { Id = 3, Nombre = "Concurrencia" } },
            tipos);
    }

    [Fact]
    public void CatalogoMetrica_ContieneLasSeisMetricasIniciales()
    {
        using var context = _database.CreateContext();

        var metricas = context.Metricas.OrderBy(m => m.Id).Select(m => new { m.Id, m.Nombre, m.Unidad }).ToList();

        Assert.Equal(
            new[]
            {
                new { Id = 1, Nombre = "CPU", Unidad = "%" },
                new { Id = 2, Nombre = "Memoria RAM", Unidad = "MB" },
                new { Id = 3, Nombre = "Tiempo de respuesta", Unidad = "ms" },
                new { Id = 4, Nombre = "Latencia", Unidad = "ms" },
                new { Id = 5, Nombre = "Throughput", Unidad = "req/s" },
                new { Id = 6, Nombre = "Tamaño de la aplicación", Unidad = "MB" }
            },
            metricas);
    }

    [Theory]
    [InlineData(typeof(ConfiguracionPrueba), "ConfiguracionId", "TipoPruebaId")]
    [InlineData(typeof(ConfiguracionMetrica), "ConfiguracionId", "MetricaId")]
    public void TablasDeUnion_UsanClaveCompuesta(Type entidad, string primera, string segunda)
    {
        using var context = _database.CreateContext();

        var clave = context.Model.FindEntityType(entidad)!.FindPrimaryKey()!;

        Assert.Equal(new[] { primera, segunda }, clave.Properties.Select(p => p.Name));
    }

    [Fact]
    public void ConfiguracionPrueba_DuplicadaEsRechazada()
    {
        using (var context = _database.CreateContext())
        {
            var proyecto = NuevoProyecto();
            var configuracion = new ConfiguracionEvaluacion
            {
                Proyecto = proyecto,
                Migracion = NuevaMigracion(proyecto),
                Nombre = "Evaluación",
                NumeroEjecuciones = 1,
                RutaObjetivo = "/",
                FechaCreacion = DateTime.UtcNow,
                Estado = "Configurada"
            };
            configuracion.ConfiguracionesPrueba.Add(new ConfiguracionPrueba { TipoPruebaId = 1 });
            context.ConfiguracionesEvaluacion.Add(configuracion);
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        var id = otro.ConfiguracionesEvaluacion.Select(c => c.Id).Single();
        otro.ConfiguracionesPrueba.Add(new ConfiguracionPrueba { ConfiguracionId = id, TipoPruebaId = 1 });

        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void CampoRequerido_NuloEsRechazado()
    {
        using var context = _database.CreateContext();
        var proyecto = NuevoProyecto();
        proyecto.RutaOriginal = null!;
        context.ProyectosDotNet.Add(proyecto);

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void ClaveForanea_InexistenteEsRechazada()
    {
        using var context = _database.CreateContext();
        context.Migraciones.Add(new Migracion
        {
            ProyectoId = 999,
            VersionOrigen = "net8.0",
            VersionDestino = "net10.0",
            Estado = "Pendiente",
            FechaInicio = DateTime.UtcNow
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void NombreDeMetrica_DuplicadoEsRechazado()
    {
        using var context = _database.CreateContext();
        context.Metricas.Add(new Metrica { Nombre = "CPU", Unidad = "%" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void NombreDeTipoPrueba_DuplicadoEsRechazado()
    {
        using var context = _database.CreateContext();
        context.TiposPrueba.Add(new TipoPrueba { Nombre = "Carga" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Metrica_ConResultados_NoSePuedeEliminar()
    {
        var ejecucionId = CrearEjecucion();
        using (var context = _database.CreateContext())
        {
            context.ResultadosMetrica.Add(new ResultadoMetrica { EjecucionId = ejecucionId, MetricaId = 1, Valor = 45.5m });
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        otro.Metricas.Remove(otro.Metricas.Single(m => m.Id == 1));

        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void Ejecucion_ConResultados_NoSePuedeEliminar()
    {
        var ejecucionId = CrearEjecucion();
        using (var context = _database.CreateContext())
        {
            context.ResultadosMetrica.Add(new ResultadoMetrica { EjecucionId = ejecucionId, MetricaId = 2, Valor = 128m });
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        otro.Ejecuciones.Remove(otro.Ejecuciones.Single(e => e.Id == ejecucionId));

        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
        Assert.Equal(1, _database.CreateContext().ResultadosMetrica.Count());
    }

    [Fact]
    public void RelacionesConfiguradas_NoUsanEliminacionEnCascada()
    {
        using var context = _database.CreateContext();

        var cascadas = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.DeleteBehavior != DeleteBehavior.Restrict)
            .Select(fk => $"{fk.DeclaringEntityType.ClrType.Name} → {fk.PrincipalEntityType.ClrType.Name}")
            .ToList();

        Assert.Empty(cascadas);
    }
}
