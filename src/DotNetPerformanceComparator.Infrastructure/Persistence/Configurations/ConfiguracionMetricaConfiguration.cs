using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ConfiguracionMetricaConfiguration : IEntityTypeConfiguration<ConfiguracionMetrica>
{
    public void Configure(EntityTypeBuilder<ConfiguracionMetrica> builder)
    {
        builder.ToTable("ConfiguracionMetrica");
        builder.HasKey(cm => new { cm.ConfiguracionId, cm.MetricaId });

        builder.HasOne(cm => cm.Configuracion)
            .WithMany(c => c.ConfiguracionesMetrica)
            .HasForeignKey(cm => cm.ConfiguracionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cm => cm.Metrica)
            .WithMany(m => m.ConfiguracionesMetrica)
            .HasForeignKey(cm => cm.MetricaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
