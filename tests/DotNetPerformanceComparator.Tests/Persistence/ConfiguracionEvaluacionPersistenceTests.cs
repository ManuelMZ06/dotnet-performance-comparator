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

    private ConfiguracionEvaluacion NuevaConfiguracion(int proyectoId, int migracionId, int tipoPruebaId = 1, int numeroEjecuciones = 3) => new()
    {
        ProyectoId = proyectoId,
        MigracionId = migracionId,
        TipoPruebaId = tipoPruebaId,
        Nombre = "Evaluación comparativa 01",
        NumeroEjecuciones = numeroEjecuciones,
        RutaObjetivo = "/api/productos",
        FechaCreacion = DateTime.UtcNow,
        Estado = "Configurada"
    };

    [Fact]
    public void Agregado_SeGuardaConUnSoloSaveChangesConUnUnicoTipoYSusParametros()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();

        using (var context = _database.CreateContext())
        {
            var config = NuevaConfiguracion(proyectoId, migracionId, tipoPruebaId: 1); // Carga
            config.Observaciones = "Condiciones equivalentes";
            config.Parametros.Add(new ParametroEvaluacion { Clave = "UsuariosVirtuales", Valor = "50" });
            config.Parametros.Add(new ParametroEvaluacion { Clave = "DuracionSegundos", Valor = "120" });
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = 1 });
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = 2 });

            context.ConfiguracionesEvaluacion.Add(config);
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        var cargada = verificacion.ConfiguracionesEvaluacion
            .Include(c => c.TipoPrueba)
            .Include(c => c.Parametros)
            .Include(c => c.ConfiguracionesMetrica)
            .Single();

        Assert.Equal("Carga", cargada.TipoPrueba.Nombre);
        Assert.Equal("/api/productos", cargada.RutaObjetivo);
        Assert.Equal("Condiciones equivalentes", cargada.Observaciones);
        Assert.Equal(new[] { "DuracionSegundos", "UsuariosVirtuales" }, cargada.Parametros.Select(p => p.Clave).Order());
        Assert.Equal(2, cargada.ConfiguracionesMetrica.Count);
    }

    [Fact]
    public void VariasConfiguraciones_PuedenUsarElMismoTipoPrueba()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using (var context = _database.CreateContext())
        {
            context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId, tipoPruebaId: 1));
            context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId, tipoPruebaId: 1));
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        Assert.Equal(2, verificacion.ConfiguracionesEvaluacion.Count(c => c.TipoPruebaId == 1));
    }

    [Fact]
    public void RutaObjetivoNula_EsRechazada()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using var context = _database.CreateContext();
        var config = NuevaConfiguracion(proyectoId, migracionId);
        config.RutaObjetivo = null!;
        context.ConfiguracionesEvaluacion.Add(config);

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Observaciones_EsOpcional()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using (var context = _database.CreateContext())
        {
            var config = NuevaConfiguracion(proyectoId, migracionId);
            config.Observaciones = null;
            context.ConfiguracionesEvaluacion.Add(config);
            context.SaveChanges();
        }

        using var verificacion = _database.CreateContext();
        Assert.Null(verificacion.ConfiguracionesEvaluacion.Single().Observaciones);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void NumeroEjecucionesFueraDe1a3_EsRechazadoPorElCheck(int numero)
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        using var context = _database.CreateContext();
        context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId, numeroEjecuciones: numero));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void RelacionConTipoPrueba_UsaRestrictYNoHayTablaConfiguracionPrueba()
    {
        using var context = _database.CreateContext();

        var fk = context.Model.FindEntityType(typeof(ConfiguracionEvaluacion))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(TipoPrueba));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.Equal(nameof(ConfiguracionEvaluacion.TipoPruebaId), Assert.Single(fk.Properties).Name);

        Assert.DoesNotContain(context.Model.GetEntityTypes(), e => e.ClrType.Name == "ConfiguracionPrueba");
    }

    [Fact]
    public void ParametroEvaluacion_SeRelacionaSoloPorConfiguracionId()
    {
        using var context = _database.CreateContext();
        var parametro = context.Model.FindEntityType(typeof(ParametroEvaluacion))!;

        var fk = Assert.Single(parametro.GetForeignKeys());
        Assert.Equal(typeof(ConfiguracionEvaluacion), fk.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(ParametroEvaluacion.ConfiguracionId), Assert.Single(fk.Properties).Name);
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        Assert.DoesNotContain(parametro.GetProperties(), p => p.Name == "TipoPruebaId");
    }
}
