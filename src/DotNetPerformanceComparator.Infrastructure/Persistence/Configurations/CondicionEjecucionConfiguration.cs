using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class CondicionEjecucionConfiguration : IEntityTypeConfiguration<CondicionEjecucion>
{
    public void Configure(EntityTypeBuilder<CondicionEjecucion> builder)
    {
        builder.ToTable("CondicionEjecucion");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Clave).IsRequired();
        builder.Property(c => c.Valor).IsRequired();

        builder.HasOne(c => c.Ejecucion)
            .WithMany(e => e.Condiciones)
            .HasForeignKey(c => c.EjecucionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
