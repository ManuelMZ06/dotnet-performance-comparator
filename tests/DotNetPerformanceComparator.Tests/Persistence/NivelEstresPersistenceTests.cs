using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class NivelEstresPersistenceTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private int CrearEjecucion()
    {
        using var context = _database.CreateContext();
        var proyecto = new ProyectoDotNet { Nombre = "App", RutaOriginal = @"C:\App\App.csproj", VersionBase = ".NET 8", EstadoValidacion = "Valido", FechaRegistro = DateTime.UtcNow };
        proyecto.Migraciones.Add(new Migracion { VersionOrigen = ".NET 8", VersionDestino = ".NET 10", Estado = "Exitosa", FechaInicio = DateTime.UtcNow });
        context.ProyectosDotNet.Add(proyecto);
        context.SaveChanges();
        var config = new ConfiguracionEvaluacion
        {
            ProyectoId = proyecto.Id, MigracionId = proyecto.Migraciones.First().Id, TipoPruebaId = 2,
            Nombre = "Estrés", NumeroEjecuciones = 1, RutaObjetivo = "/", Estado = "EnEjecucion", FechaCreacion = DateTime.UtcNow
        };
        context.ConfiguracionesEvaluacion.Add(config);
        context.SaveChanges();
        var ejecucion = new Ejecucion
        {
            ConfiguracionId = config.Id, VersionDotNet = ".NET 8", NumeroEjecucion = 1,
            FechaInicio = DateTime.UtcNow, Estado = EstadosEjecucion.EnProceso
        };
        context.Ejecuciones.Add(ejecucion);
        context.SaveChanges();
        return ejecucion.Id;
    }

    private static NivelEstres Nivel(int ejecucionId, int numero = 1, int usuarios = 10) => new()
    {
        EjecucionId = ejecucionId, NumeroNivel = numero, UsuariosObjetivo = usuarios,
        FechaInicio = DateTime.UtcNow, Estado = EstadosNivelEstres.Completado, SolicitudesExitosas = 100, SolicitudesFallidas = 0
    };

    [Fact]
    public void NivelConResultados_SeGuardaYRecupera()
    {
        var ejecucionId = CrearEjecucion();
        int nivelId;
        using (var context = _database.CreateContext())
        {
            var nivel = Nivel(ejecucionId);
            nivel.Resultados.Add(new ResultadoMetricaNivelEstres { MetricaId = 1, Valor = 0.25m });
            nivel.Resultados.Add(new ResultadoMetricaNivelEstres { MetricaId = 4, Valor = 12.5m });
            context.NivelesEstres.Add(nivel);
            context.SaveChanges();
            nivelId = nivel.Id;
        }

        using var verificacion = _database.CreateContext();
        var cargado = verificacion.NivelesEstres.Include(n => n.Resultados).Single(n => n.Id == nivelId);
        Assert.Equal(2, cargado.Resultados.Count);
        Assert.Equal(0.25m, cargado.Resultados.Single(r => r.MetricaId == 1).Valor);
    }

    [Fact]
    public void Nivel_SinEjecucionValida_EsRechazado()
    {
        using var context = _database.CreateContext();
        context.NivelesEstres.Add(Nivel(ejecucionId: 9999));
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void ResultadoNivel_SinMetricaValida_EsRechazado()
    {
        var ejecucionId = CrearEjecucion();
        int nivelId;
        using (var context = _database.CreateContext())
        {
            var nivel = Nivel(ejecucionId);
            context.NivelesEstres.Add(nivel);
            context.SaveChanges();
            nivelId = nivel.Id;
        }

        using var otro = _database.CreateContext();
        otro.ResultadosMetricaNivelEstres.Add(new ResultadoMetricaNivelEstres { NivelEstresId = nivelId, MetricaId = 9999, Valor = 1m });
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void Nivel_NumeroDuplicadoEnEjecucion_EsRechazado()
    {
        var ejecucionId = CrearEjecucion();
        using (var context = _database.CreateContext())
        {
            context.NivelesEstres.Add(Nivel(ejecucionId, numero: 1));
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        otro.NivelesEstres.Add(Nivel(ejecucionId, numero: 1));
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void ResultadoNivel_MetricaDuplicadaEnNivel_EsRechazado()
    {
        var ejecucionId = CrearEjecucion();
        int nivelId;
        using (var context = _database.CreateContext())
        {
            var nivel = Nivel(ejecucionId);
            nivel.Resultados.Add(new ResultadoMetricaNivelEstres { MetricaId = 1, Valor = 1m });
            context.NivelesEstres.Add(nivel);
            context.SaveChanges();
            nivelId = nivel.Id;
        }

        using var otro = _database.CreateContext();
        otro.ResultadosMetricaNivelEstres.Add(new ResultadoMetricaNivelEstres { NivelEstresId = nivelId, MetricaId = 1, Valor = 2m });
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void NivelesCompletados_PersistenAunqueUnoPosteriorNoSeAgregue()
    {
        var ejecucionId = CrearEjecucion();
        // Niveles 1 y 2 completados (guardados incrementalmente).
        using (var context = _database.CreateContext())
        {
            context.NivelesEstres.Add(Nivel(ejecucionId, numero: 1));
            context.SaveChanges();
        }
        using (var context = _database.CreateContext())
        {
            context.NivelesEstres.Add(Nivel(ejecucionId, numero: 2));
            context.SaveChanges();
        }

        // Un "fallo" posterior no borra lo ya guardado.
        using var verificacion = _database.CreateContext();
        Assert.Equal(2, verificacion.NivelesEstres.Count(n => n.EjecucionId == ejecucionId));
    }

    [Fact]
    public void RestrictImpideBorrarEjecucionConNiveles()
    {
        var ejecucionId = CrearEjecucion();
        using (var context = _database.CreateContext())
        {
            context.NivelesEstres.Add(Nivel(ejecucionId));
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        var ejecucion = otro.Ejecuciones.Single(e => e.Id == ejecucionId);
        otro.Ejecuciones.Remove(ejecucion);
        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }
}
