using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ConfiguracionEvaluacionConfiguration : IEntityTypeConfiguration<ConfiguracionEvaluacion>
{
    public void Configure(EntityTypeBuilder<ConfiguracionEvaluacion> builder)
    {
        builder.ToTable("ConfiguracionEvaluacion");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroEjecuciones).IsRequired();
        builder.Property(c => c.FechaCreacion).IsRequired();
        builder.Property(c => c.Estado).IsRequired();

        builder.HasOne(c => c.Proyecto)
            .WithMany(p => p.ConfiguracionesEvaluacion)
            .HasForeignKey(c => c.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
