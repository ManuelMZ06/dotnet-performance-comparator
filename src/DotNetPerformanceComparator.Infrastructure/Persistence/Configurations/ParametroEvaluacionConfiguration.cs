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

        builder.Property(p => p.TipoPruebaId).IsRequired();
        builder.Property(p => p.Clave).IsRequired();
        builder.Property(p => p.Valor).IsRequired();

        // Cada parámetro pertenece a una ConfiguracionPrueba concreta (clave compuesta),
        // de modo que la base garantiza a qué tipo de prueba corresponde.
        builder.HasOne(p => p.ConfiguracionPrueba)
            .WithMany(cp => cp.Parametros)
            .HasForeignKey(p => new { p.ConfiguracionId, p.TipoPruebaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
