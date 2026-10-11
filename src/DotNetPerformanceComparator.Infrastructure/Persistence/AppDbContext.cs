using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceComparator.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProyectoDotNet> ProyectosDotNet => Set<ProyectoDotNet>();
    public DbSet<Migracion> Migraciones => Set<Migracion>();
    public DbSet<TipoPrueba> TiposPrueba => Set<TipoPrueba>();
    public DbSet<ConfiguracionEvaluacion> ConfiguracionesEvaluacion => Set<ConfiguracionEvaluacion>();
    public DbSet<ParametroEvaluacion> ParametrosEvaluacion => Set<ParametroEvaluacion>();
    public DbSet<Metrica> Metricas => Set<Metrica>();
    public DbSet<ConfiguracionMetrica> ConfiguracionesMetrica => Set<ConfiguracionMetrica>();
    public DbSet<Ejecucion> Ejecuciones => Set<Ejecucion>();
    public DbSet<CondicionEjecucion> CondicionesEjecucion => Set<CondicionEjecucion>();
    public DbSet<ResultadoMetrica> ResultadosMetrica => Set<ResultadoMetrica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
