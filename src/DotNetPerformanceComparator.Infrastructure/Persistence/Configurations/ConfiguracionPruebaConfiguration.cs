using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ConfiguracionPruebaConfiguration : IEntityTypeConfiguration<ConfiguracionPrueba>
{
    public void Configure(EntityTypeBuilder<ConfiguracionPrueba> builder)
    {
        builder.ToTable("ConfiguracionPrueba");
        builder.HasKey(cp => new { cp.ConfiguracionId, cp.TipoPruebaId });

        builder.HasOne(cp => cp.Configuracion)
            .WithMany(c => c.ConfiguracionesPrueba)
            .HasForeignKey(cp => cp.ConfiguracionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cp => cp.TipoPrueba)
            .WithMany(t => t.ConfiguracionesPrueba)
            .HasForeignKey(cp => cp.TipoPruebaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
