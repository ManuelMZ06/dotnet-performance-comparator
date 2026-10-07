using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class EjecucionConfiguration : IEntityTypeConfiguration<Ejecucion>
{
    public void Configure(EntityTypeBuilder<Ejecucion> builder)
    {
        builder.ToTable("Ejecucion");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.VersionDotNet).IsRequired();
        builder.Property(e => e.NumeroEjecucion).IsRequired();
        builder.Property(e => e.FechaInicio).IsRequired();
        builder.Property(e => e.Estado).IsRequired();

        builder.HasOne(e => e.Configuracion)
            .WithMany(c => c.Ejecuciones)
            .HasForeignKey(e => e.ConfiguracionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
