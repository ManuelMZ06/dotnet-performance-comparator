using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ProyectoDotNetConfiguration : IEntityTypeConfiguration<ProyectoDotNet>
{
    public void Configure(EntityTypeBuilder<ProyectoDotNet> builder)
    {
        builder.ToTable("ProyectoDotNet");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre).IsRequired();
        builder.Property(p => p.RutaOriginal).IsRequired();
        builder.Property(p => p.VersionBase).IsRequired();
        builder.Property(p => p.EstadoValidacion).IsRequired();
        builder.Property(p => p.FechaRegistro).IsRequired();
    }
}
