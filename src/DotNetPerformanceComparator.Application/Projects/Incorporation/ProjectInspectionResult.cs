namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

public enum ValidationStatus
{
    Pendiente,
    Correcto,
    Error
}

public enum ProjectInspectionState
{
    Pendiente,
    Valido,
    NoValido
}

public sealed record ProjectValidation(string Descripcion, ValidationStatus Estado);

/// <summary>
/// Resultado interpretado de la inspección (CU-01). Contiene información transitoria
/// para M-02 que no se persiste (tipo de aplicación, framework, carpetas y archivos).
/// </summary>
public sealed record ProjectInspectionResult(
    string? RutaProyecto,
    string? NombreSugerido,
    string? TipoAplicacion,
    string? VersionDetectada,
    string? FrameworkObjetivo,
    IReadOnlyList<string> CarpetasClave,
    IReadOnlyList<string> ArchivosDetectados,
    ProjectInspectionState Estado,
    IReadOnlyList<ProjectValidation> Validaciones,
    string Mensaje)
{
    public bool EsValido => Estado == ProjectInspectionState.Valido;
}
