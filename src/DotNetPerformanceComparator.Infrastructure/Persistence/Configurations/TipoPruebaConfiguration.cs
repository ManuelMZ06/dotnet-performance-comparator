using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class TipoPruebaConfiguration : IEntityTypeConfiguration<TipoPrueba>
{
    public void Configure(EntityTypeBuilder<TipoPrueba> builder)
    {
        builder.ToTable("TipoPrueba");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nombre).IsRequired();
        builder.HasIndex(t => t.Nombre).IsUnique();

        builder.HasData(
            new TipoPrueba { Id = 1, Nombre = "Carga", Descripcion = "Evalúa el comportamiento de la aplicación bajo una carga de trabajo esperada." },
            new TipoPrueba { Id = 2, Nombre = "Estrés", Descripcion = "Evalúa el comportamiento de la aplicación bajo una carga superior a la esperada." },
            new TipoPrueba { Id = 3, Nombre = "Concurrencia", Descripcion = "Evalúa el comportamiento de la aplicación ante solicitudes simultáneas." });
    }
}
