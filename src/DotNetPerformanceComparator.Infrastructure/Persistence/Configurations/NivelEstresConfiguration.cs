using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class NivelEstresConfiguration : IEntityTypeConfiguration<NivelEstres>
{
    public void Configure(EntityTypeBuilder<NivelEstres> builder)
    {
        builder.ToTable("NivelEstres");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.NumeroNivel).IsRequired();
        builder.Property(n => n.UsuariosObjetivo).IsRequired();
        builder.Property(n => n.FechaInicio).IsRequired();
        builder.Property(n => n.Estado).IsRequired();

        // Dentro de una ejecución, cada número de nivel aparece una sola vez.
        builder.HasIndex(n => new { n.EjecucionId, n.NumeroNivel }).IsUnique();

        builder.HasOne(n => n.Ejecucion)
            .WithMany(e => e.Niveles)
            .HasForeignKey(n => n.EjecucionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
