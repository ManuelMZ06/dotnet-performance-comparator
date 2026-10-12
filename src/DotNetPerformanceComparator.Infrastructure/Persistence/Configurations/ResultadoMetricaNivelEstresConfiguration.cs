using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ResultadoMetricaNivelEstresConfiguration : IEntityTypeConfiguration<ResultadoMetricaNivelEstres>
{
    public void Configure(EntityTypeBuilder<ResultadoMetricaNivelEstres> builder)
    {
        builder.ToTable("ResultadoMetricaNivelEstres");
        builder.HasKey(r => r.Id);

        // SQLite almacena decimal como TEXT, sin pérdida de precisión (ver D-11 en docs/PROJECT_CONTEXT.md).
        builder.Property(r => r.Valor).IsRequired();

        // Un nivel tiene a lo sumo un resultado por métrica.
        builder.HasIndex(r => new { r.NivelEstresId, r.MetricaId }).IsUnique();

        builder.HasOne(r => r.NivelEstres)
            .WithMany(n => n.Resultados)
            .HasForeignKey(r => r.NivelEstresId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Metrica)
            .WithMany(m => m.ResultadosNivel)
            .HasForeignKey(r => r.MetricaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
