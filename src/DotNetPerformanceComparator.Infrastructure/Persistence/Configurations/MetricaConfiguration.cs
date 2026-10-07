using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotNetPerformanceComparator.Infrastructure.Persistence.Configurations;

public class MetricaConfiguration : IEntityTypeConfiguration<Metrica>
{
    public void Configure(EntityTypeBuilder<Metrica> builder)
    {
        builder.ToTable("Metrica");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Nombre).IsRequired();
        builder.Property(m => m.Unidad).IsRequired();
        builder.HasIndex(m => m.Nombre).IsUnique();

        builder.HasData(
            new Metrica { Id = 1, Nombre = "CPU", Unidad = "%", Descripcion = "Uso de CPU." },
            new Metrica { Id = 2, Nombre = "Memoria RAM", Unidad = "MB", Descripcion = "Consumo de memoria RAM." },
            new Metrica { Id = 3, Nombre = "Tiempo de respuesta", Unidad = "ms", Descripcion = "Tiempo de respuesta de las solicitudes." },
            new Metrica { Id = 4, Nombre = "Latencia", Unidad = "ms", Descripcion = "Latencia de las solicitudes." },
            new Metrica { Id = 5, Nombre = "Throughput", Unidad = "req/s", Descripcion = "Solicitudes procesadas por segundo." },
            new Metrica { Id = 6, Nombre = "Tamaño de la aplicación", Unidad = "MB", Descripcion = "Tamaño de la aplicación." });
    }
}
