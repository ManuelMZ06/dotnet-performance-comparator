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

    private ConfiguracionEvaluacion NuevaConfiguracion(int proyectoId, int migracionId, int numeroEjecuciones = 3) => new()
    {
        ProyectoId = proyectoId,
        MigracionId = migracionId,
        Nombre = "Evaluación comparativa 01",
        NumeroEjecuciones = numeroEjecuciones,
        RutaObjetivo = "/api/productos",
        FechaCreacion = DateTime.UtcNow,
        Estado = "Configurada"
    };

    [Fact]
    public void Agregado_SeGuardaConUnSoloSaveChangesYRecuperaParametrosPorTipo()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();

        using (var context = _database.CreateContext())
        {
            var config = NuevaConfiguracion(proyectoId, migracionId);
            config.Observaciones = "Condiciones equivalentes";
            config.ConfiguracionesPrueba.Add(new ConfiguracionPrueba
            {
                TipoPruebaId = 1, // Carga
                Parametros = { new ParametroEvaluacion { TipoPruebaId = 1, Clave = "UsuariosVirtuales", Valor = "50" } }
            });
            config.ConfiguracionesPrueba.Add(new ConfiguracionPrueba
            {
                TipoPruebaId = 2, // Estrés
                Parametros = { new ParametroEvaluacion { TipoPruebaId = 2, Clave = "UsuariosIniciales", Valor = "10" } }
            });
            config.ConfiguracionesMetrica.Add(new ConfiguracionMetrica { MetricaId = 1 });

            context.ConfiguracionesEvaluacion.Add(config);
            Assert.Equal(1, context.SaveChanges() > 0 ? 1 : 0); // un único SaveChanges
        }

        using var verificacion = _database.CreateContext();
        var cargada = verificacion.ConfiguracionesEvaluacion
            .Include(c => c.ConfiguracionesPrueba).ThenInclude(cp => cp.Parametros)
            .Single();

        Assert.Equal("/api/productos", cargada.RutaObjetivo);
        Assert.Equal("Condiciones equivalentes", cargada.Observaciones);
        var carga = cargada.ConfiguracionesPrueba.Single(cp => cp.TipoPruebaId == 1);
        var estres = cargada.ConfiguracionesPrueba.Single(cp => cp.TipoPruebaId == 2);
        Assert.Equal("UsuariosVirtuales", Assert.Single(carga.Parametros).Clave);
        Assert.Equal("UsuariosIniciales", Assert.Single(estres.Parametros).Clave);
        // Los parámetros no se mezclan entre tipos.
        Assert.All(carga.Parametros, p => Assert.Equal(1, p.TipoPruebaId));
        Assert.All(estres.Parametros, p => Assert.Equal(2, p.TipoPruebaId));
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
            config.ConfiguracionesPrueba.Add(new ConfiguracionPrueba { TipoPruebaId = 1 });
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
        context.ConfiguracionesEvaluacion.Add(NuevaConfiguracion(proyectoId, migracionId, numero));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Parametro_ParaTipoNoIncluidoEnLaConfiguracion_EsRechazado()
    {
        var (proyectoId, migracionId) = CrearProyectoConMigracion();
        int configId;
        using (var context = _database.CreateContext())
        {
            var config = NuevaConfiguracion(proyectoId, migracionId);
            config.ConfiguracionesPrueba.Add(new ConfiguracionPrueba { TipoPruebaId = 1 }); // solo Carga
            context.ConfiguracionesEvaluacion.Add(config);
            context.SaveChanges();
            configId = config.Id;
        }

        using var otro = _database.CreateContext();
        // Parámetro referido al tipo 2 (Estrés), que no es una ConfiguracionPrueba de esta configuración.
        otro.ParametrosEvaluacion.Add(new ParametroEvaluacion { ConfiguracionId = configId, TipoPruebaId = 2, Clave = "X", Valor = "1" });

        Assert.Throws<DbUpdateException>(() => otro.SaveChanges());
    }

    [Fact]
    public void ParametroEvaluacion_UsaClaveForaneaCompuestaHaciaConfiguracionPrueba()
    {
        using var context = _database.CreateContext();

        var fk = context.Model.FindEntityType(typeof(ParametroEvaluacion))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(ConfiguracionPrueba));

        Assert.Equal(new[] { "ConfiguracionId", "TipoPruebaId" }, fk.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }
}
