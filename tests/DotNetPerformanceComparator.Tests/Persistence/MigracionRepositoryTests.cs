using DotNetPerformanceComparator.Domain.Entities;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class MigracionRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private sealed class Factory(SqliteInMemoryDatabase database) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => database.CreateContext();
    }

    private int CrearProyecto(string nombre)
    {
        using var context = _database.CreateContext();
        var proyecto = new ProyectoDotNet
        {
            Nombre = nombre,
            RutaOriginal = $@"C:\Temp\{nombre}\{nombre}.csproj",
            VersionBase = ".NET 8",
            EstadoValidacion = "Valido",
            FechaRegistro = DateTime.UtcNow
        };
        context.ProyectosDotNet.Add(proyecto);
        context.SaveChanges();
        return proyecto.Id;
    }

    [Fact]
    public async Task AddYUpdate_PersistenEstadoFechasYRelacionConProyecto()
    {
        var proyectoId = CrearProyecto("App");
        var repositorio = new MigracionRepository(new Factory(_database));
        var inicio = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var migracion = new Migracion
        {
            ProyectoId = proyectoId,
            VersionOrigen = ".NET 8",
            VersionDestino = ".NET 10",
            Estado = "EnProceso",
            FechaInicio = inicio
        };

        await repositorio.AddAsync(migracion);
        Assert.True(migracion.Id > 0);

        migracion.Estado = "Exitosa";
        migracion.FechaFin = inicio.AddMinutes(2);
        migracion.RutaMigrada = @"C:\Salida\AppNet10";
        migracion.Detalle = "SDK utilizado: 10.0.400.";
        await repositorio.UpdateAsync(migracion);

        using var verificacion = _database.CreateContext();
        var guardada = verificacion.Migraciones.Include(m => m.Proyecto).Single();
        Assert.Equal("Exitosa", guardada.Estado);
        Assert.Equal(inicio, guardada.FechaInicio);
        Assert.Equal(inicio.AddMinutes(2), guardada.FechaFin);
        Assert.Equal(@"C:\Salida\AppNet10", guardada.RutaMigrada);
        Assert.Equal("SDK utilizado: 10.0.400.", guardada.Detalle);
        Assert.Equal("App", guardada.Proyecto.Nombre);
    }

    [Fact]
    public async Task ProyectoRepository_ConsultaTodosOrdenadosYPorId()
    {
        CrearProyecto("Zeta");
        var alfa = CrearProyecto("Alfa");
        var repositorio = new ProyectoDotNetRepository(new Factory(_database));

        var todos = await repositorio.GetAllAsync();
        var porId = await repositorio.GetByIdAsync(alfa);

        Assert.Equal(new[] { "Alfa", "Zeta" }, todos.Select(p => p.Nombre));
        Assert.Equal("Alfa", porId!.Nombre);
        Assert.Null(await repositorio.GetByIdAsync(999));
    }
}
