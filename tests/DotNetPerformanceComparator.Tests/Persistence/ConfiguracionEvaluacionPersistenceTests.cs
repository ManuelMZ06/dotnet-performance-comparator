using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class ConfiguracionEvaluacionPersistenceTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private (int ProyectoId, int MigracionId) CrearProyectoConMigracion()
    {
        using var context = _database.CreateContext();
        var proyecto = new ProyectoDotNet
        {
            Nombre = "App", RutaOriginal = @"C:\App\App.csproj", VersionBase = ".NET 8",
            EstadoValidacion = "Valido", FechaRegistro = DateTime.UtcNow
        };
        proyecto.Migraciones.Add(new Migracion
        {
            VersionOrigen = ".NET 8", VersionDestino = ".NET 10", Estado = "Exitosa", FechaInicio = DateTime.UtcNow
        });
        context.ProyectosDotNet.Add(proyecto);
        context.SaveChanges();
        return (proyecto.Id, proyecto.Migraciones.First().Id);
    }

    private ConfiguracionEvaluacion NuevaConfiguracion(int proyectoId, int migracionId) => new()
    {
        ProyectoId = proyectoId,
        MigracionId = migracionId,
        Nombre = "Evaluación comparativa 01",
        NumeroEjecuciones = 3,
        Estado = "Configurada",
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public void Agregado_SePersisteYRecuperaConNavegacionAMigracion()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();

        using (var context = _database.CreateContext())
        {
            var config = NuevaConfiguracion(proyectoId, migracionId);
            config.ConfiguracionesPrueba.Add(new ConfiguracionPrueba { TipoPruebaId = 1 });
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = 3 });
            config.Parametros.Add(new ParametroEvaluacion { Clave = "UsuariosVirtuales", Valor = "50" });
            context.ConfiguracionesEvaluacion.Add(config);
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        var cargada = verificacion.ConfiguracionesEvaluacion
            .Include(c => c.Migracion)
            .Include(c => c.ConfiguracionesPrueba)
            .Include(c => c.ConfiguracionesMetrica)
            .Include(c => c.Parametros)
            .Single();

        Assert.Equal("Evaluación comparativa 01", cargada.Nombre);
        Assert.Equal(migracionId, cargada.MigracionId);
        Assert.Equal("Exitosa", cargada.Migracion.Estado);
        Assert.Equal(1, Assert.Single(cargada.ConfiguracionesPrueba).TipoPruebaId);
        Assert.Equal(3, Assert.Single(cargada.ConfiguracionesMetrica).MetricaId);
        Assert.Equal("50", Assert.Single(cargada.Parametros).Valor);
    }

    [Fact]
    public void NombreNulo_EsRechazado()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using var context = _database.CreateContext();
        var config = NuevaConfiguracion(proyectoId, migracionId);
        config.Nombre = null!;
        context.ConfiguracionesEvaluacion.Add(config);

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void MigracionIdInexistente_EsRechazado()
    {
        var (proyectoId, _) = CrearProyectoConMigracion();
        using var context = _database.CreateContext();
        context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId: 999));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Migracion_ConConfiguraciones_NoSePuedeEliminar()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using (var context = _database.CreateContext())
        {
            context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId));
            context.SaveChanges();
        }

        using var otro = _database.CreateContext();
        otro.Migraciones.Remove(otro.Migraciones.Single(m => m.Id == migracionId));

        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void RelacionConMigracion_UsaRestrict()
    {
        using var context = _database.CreateContext();

        var fk = context.Model.FindEntityType(typeof(ConfiguracionEvaluacion))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Migracion));

        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.Equal(nameof(ConfiguracionEvaluacion.MigracionId), Assert.Single(fk.Properties).Name);
    }
}
