namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

public sealed record ProyectoResumen(int Id, string Nombre, string RutaOriginal);

public sealed record MigrarProyectoRequest(int? ProyectoId, string? DirectorioSalida);

/// <summary>Resumen de una <c>Migracion</c> registrada, para mostrar en M-03.</summary>
public sealed record MigracionResumen(
    int Id,
    string Proyecto,
    string VersionOrigen,
    string VersionDestino,
    string? SdkUtilizado,
    string? RutaMigrada,
    DateTime FechaInicio,
    DateTime? FechaFin,
    string Estado,
    string? Detalle);

/// <param name="Exito">La copia experimental se preparó, restauró y compiló.</param>
/// <param name="Mensaje">Mensaje comprensible para el Usuario.</param>
/// <param name="Migracion">Registro persistido; null si la migración no llegó a comenzar.</param>
public sealed record MigrarProyectoResult(bool Exito, string Mensaje, MigracionResumen? Migracion = null);
