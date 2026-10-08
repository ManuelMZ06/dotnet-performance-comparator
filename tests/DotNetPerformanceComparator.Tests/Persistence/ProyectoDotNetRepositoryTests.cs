using DotNetPerformanceComparator.Domain.Entities;
using DotNetPerformanceComparator.Infrastructure.Persistence;
using DotNetPerformanceComparator.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Tests.Persistence;

public sealed class ProyectoDotNetRepositoryTests : IDisposable
{
    private readonly SqliteInMemoryDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>Fábrica que registra los contextos creados para verificar su vida corta.</summary>
    private sealed class TrackingFactory(SqliteInMemoryDatabase database) : IDbContextFactory<AppDbContext>
    {
        public List<AppDbContext> Creados { get; } = [];

        public AppDbContext CreateDbContext()
        {
            var context = database.CreateContext();
            Creados.Add(context);
            return context;
        }
    }

    [Fact]
    public async Task AddAsync_PersisteElProyectoConContextoDeVidaCorta()
    {
        var factory = new TrackingFactory(_database);
        var repository = new ProyectoDotNetRepository(factory);
        var fecha = new DateTime(2026, 10, 7, 15, 30, 0, DateTimeKind.Utc);

        await repository.AddAsync(new ProyectoDotNet
        {
            Nombre = "MiAplicacion",
            RutaOriginal = @"C:\Temp\MiAplicacion\MiAplicacion.csproj",
            VersionBase = ".NET 8",
            EstadoValidacion = "Valido",
            FechaRegistro = fecha
        });

        var contexto = Assert.Single(factory.Creados);
        Assert.Throws<ObjectDisposedException>(() => contexto.ProyectosDotNet.Count());

        using var verificacion = _database.CreateContext();
        var guardado = Assert.Single(verificacion.ProyectosDotNet);
        Assert.Equal("MiAplicacion", guardado.Nombre);
        Assert.Equal(@"C:\Temp\MiAplicacion\MiAplicacion.csproj", guardado.RutaOriginal);
        Assert.Equal(".NET 8", guardado.VersionBase);
        Assert.Equal("Valido", guardado.EstadoValidacion);
        Assert.Equal(fecha, guardado.FechaRegistro);
    }

    [Fact]
    public void ProyectoDotNet_SoloPersisteLosCamposDelModeloDefinido()
    {
        using var context = _database.CreateContext();

        var columnas = context.Model.FindEntityType(typeof(ProyectoDotNet))!.GetProperties().Select(p => p.Name).Order();

        Assert.Equal(new[] { "EstadoValidacion", "FechaRegistro", "Id", "Nombre", "RutaOriginal", "VersionBase" }, columnas);
    }
}
