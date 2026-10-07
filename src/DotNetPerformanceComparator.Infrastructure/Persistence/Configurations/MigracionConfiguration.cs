using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class MigracionConfiguration : IEntityTypeConfiguration<Migracion>
{
    public void Configure(EntityTypeBuilder<Migracion> builder)
    {
        builder.ToTable("Migracion");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.VersionOrigen).IsRequired();
        builder.Property(m => m.VersionDestino).IsRequired();
        builder.Property(m => m.Estado).IsRequired();
        builder.Property(m => m.FechaInicio).IsRequired();

        builder.HasOne(m => m.Proyecto)
            .WithMany(p => p.Migraciones)
            .HasForeignKey(m => m.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
