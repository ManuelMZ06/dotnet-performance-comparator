using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ResultadoMetricaConfiguration : IEntityTypeConfiguration<ResultadoMetrica>
{
    public void Configure(EntityTypeBuilder<ResultadoMetrica> builder)
    {
        builder.ToTable("ResultadoMetrica");
        builder.HasKey(r => r.Id);

        // SQLite almacena decimal como TEXT, sin pérdida de precisión (ver D-11 en docs/PROJECT_CONTEXT.md).
        builder.Property(r => r.Valor).IsRequired();

        // Una ejecución tiene a lo sumo un resultado final por métrica.
        builder.HasIndex(r => new { r.EjecucionId, r.MetricaId }).IsUnique();

        builder.HasOne(r => r.Ejecucion)
            .WithMany(e => e.Resultados)
            .HasForeignKey(r => r.EjecucionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Metrica)
            .WithMany(m => m.Resultados)
            .HasForeignKey(r => r.MetricaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
