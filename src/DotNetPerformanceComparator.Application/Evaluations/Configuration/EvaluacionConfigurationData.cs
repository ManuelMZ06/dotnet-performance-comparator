namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

public sealed record CatalogoItem(int Id, string Nombre, string? Unidad = null);

public sealed record ProyectoOpcion(int Id, string Nombre);

/// <param name="FechaUtc">Fecha de finalización (o inicio) en UTC; la presentación la muestra en hora local.</param>
public sealed record MigracionOpcion(int Id, int ProyectoId, DateTime FechaUtc, string? RutaMigrada);

/// <summary>
/// Datos que M-04 necesita para construir la pantalla. Las migraciones exitosas se entregan
/// todas (con su <see cref="MigracionOpcion.ProyectoId"/>) y la presentación filtra por el
/// proyecto seleccionado.
/// </summary>
public sealed record EvaluacionConfigurationData(
    IReadOnlyList<ProyectoOpcion> Proyectos,
    IReadOnlyList<MigracionOpcion> MigracionesExitosas,
    IReadOnlyList<CatalogoItem> TiposPrueba,
    IReadOnlyList<CatalogoItem> Metricas);
