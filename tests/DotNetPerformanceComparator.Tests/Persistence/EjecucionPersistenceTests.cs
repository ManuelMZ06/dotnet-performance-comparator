using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class EjecucionPersistenceTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private int CrearConfiguracion()
    {
        using var context = _database.CreateContext();
        var proyecto = new ProyectoDotNet { Nombre = "App", RutaOriginal = @"C:\App\App.csproj", VersionBase = ".NET 8", EstadoValidacion = "Valido", FechaRegistro = DateTime.UtcNow };
        proyecto.Migraciones.Add(new Migracion { VersionOrigen = ".NET 8", VersionDestino = ".NET 10", Estado = "Exitosa", FechaInicio = DateTime.UtcNow });
        context.ProyectosDotNet.Add(proyecto);
        context.SaveChanges();
        var config = new ConfiguracionEvaluacion
        {
            ProyectoId = proyecto.Id, MigracionId = proyecto.Migraciones.First().Id, TipoPruebaId = 1,
            Nombre = "Carga", NumeroEjecuciones = 2, RutaObjetivo = "/", Estado = "EnEjecucion", FechaCreacion = DateTime.UtcNow
        };
        context.ConfiguracionesEvaluacion.Add(config);
        context.SaveChanges();
        return config.Id;
    }

    private Ejecucion NuevaEjecucion(int configId, string version = ".NET 8", int numero = 1) => new()
    {
        ConfiguracionId = configId, VersionDotNet = version, NumeroEjecucion = numero,
        FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow, Estado = "Completada"
    };

    [Fact]
    public void Agregado_EjecucionConCondicionesYResultados_SeGuardaYRecupera()
    {
        var configId = CrearConfiguracion();
        using (var context = _database.CreateContext())
        {
            var ejecucion = NuevaEjecucion(configId);
            ejecucion.Condiciones.Add(new CondicionEjecucion { Clave = "TargetFramework", Valor = "net8.0" });
            ejecucion.Resultados.Add(new ResultadoMetrica { MetricaId = 1, Valor = 42.5m });
            ejecucion.Resultados.Add(new ResultadoMetrica { MetricaId = 2, Valor = 128m });
            context.Ejecuciones.Add(ejecucion);
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        var cargada = verificacion.Ejecuciones.Include(e => e.Condiciones).Include(e => e.Resultados).Single();
        Assert.Equal("net8.0", Assert.Single(cargada.Condiciones).Valor);
        Assert.Equal(2, cargada.Resultados.Count);
        Assert.Equal(42.5m, cargada.Resultados.Single(r => r.MetricaId == 1).Valor);
    }

    [Fact]
    public void Ejecucion_CombinacionDuplicada_EsRechazada()
    {
        var configId = CrearConfiguracion();
        using (var context = _database.CreateContext())
        {
            context.Ejecuciones.Add(NuevaEjecucion(configId, ".NET 8", 1));
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        otro.Ejecuciones.Add(NuevaEjecucion(configId, ".NET 8", 1));
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void ResultadoMetrica_MetricaDuplicadaEnEjecucion_EsRechazada()
    {
        var configId = CrearConfiguracion();
        int ejecucionId;
        using (var context = _database.CreateContext())
        {
            var e = NuevaEjecucion(configId);
            e.Resultados.Add(new ResultadoMetrica { MetricaId = 1, Valor = 1m });
            context.Ejecuciones.Add(e);
            context.SaveChanges();
            ejecucionId = e.Id;
        }

        using var otro = _database.CreateContext();
        otro.ResultadosMetrica.Add(new ResultadoMetrica { EjecucionId = ejecucionId, MetricaId = 1, Valor = 2m });
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void VariasConfiguraciones_MismaCombinacionVersionRepeticion_SePermite()
    {
        var a = CrearConfiguracion();
        var b = CrearConfiguracion();
        using var context = _database.CreateContext();
        context.Ejecuciones.Add(NuevaEjecucion(a, ".NET 8", 1));
        context.Ejecuciones.Add(NuevaEjecucion(b, ".NET 8", 1));
        context.SaveChanges();
        Assert.Equal(2, context.Ejecuciones.Count());
    }
}
