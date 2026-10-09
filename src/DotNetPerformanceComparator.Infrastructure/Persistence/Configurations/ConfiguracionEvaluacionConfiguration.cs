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

        builder.Property(c => c.Nombre).IsRequired();
        builder.Property(c => c.NumeroEjecuciones).IsRequired();
        builder.Property(c => c.RutaObjetivo).IsRequired();
        // Observaciones: nullable, sin longitud máxima.
        builder.Property(c => c.FechaCreacion).IsRequired();
        builder.Property(c => c.Estado).IsRequired();

        // La regla 1..3 proviene del alcance de investigación (no es un límite técnico arbitrario).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ConfiguracionEvaluacion_NumeroEjecuciones",
            "\"NumeroEjecuciones\" >= 1 AND \"NumeroEjecuciones\" <= 3"));

        builder.HasOne(c => c.Proyecto)
            .WithMany(p => p.ConfiguracionesEvaluacion)
            .HasForeignKey(c => c.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Migracion)
            .WithMany(m => m.ConfiguracionesEvaluacion)
            .HasForeignKey(c => c.MigracionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
