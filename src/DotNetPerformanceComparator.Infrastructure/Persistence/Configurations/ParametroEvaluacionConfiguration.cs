using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class ParametroEvaluacionConfiguration : IEntityTypeConfiguration<ParametroEvaluacion>
{
    public void Configure(EntityTypeBuilder<ParametroEvaluacion> builder)
    {
        builder.ToTable("ParametroEvaluacion");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Clave).IsRequired();
        builder.Property(p => p.Valor).IsRequired();

        builder.HasOne(p => p.Configuracion)
            .WithMany(c => c.Parametros)
            .HasForeignKey(p => p.ConfiguracionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
