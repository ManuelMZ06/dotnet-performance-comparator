namespace DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

/// <summary>Etapas de CU-02, en orden de ejecución.</summary>
public enum MigrationStage
{
    ValidacionProyecto,
    CopiaExperimental,
    ActualizacionFramework,
    Restauracion,
    Build
}

public enum MigrationStageStatus
{
    Pendiente,
    EnProceso,
    Correcto,
    Error
}

/// <summary>
/// Actualización de progreso de la migración: cambio de estado de una etapa o una línea de bitácora.
/// </summary>
public sealed record MigrationProgress(MigrationStage? Stage, MigrationStageStatus StageStatus, string? LogLine)
{
    public static MigrationProgress StageChanged(MigrationStage stage, MigrationStageStatus status) => new(stage, status, null);

    public static MigrationProgress Log(string line) => new(null, MigrationStageStatus.Pendiente, line);
}

public static class MigrationStages
{
    public static IReadOnlyList<MigrationStage> All { get; } = Enum.GetValues<MigrationStage>();

    public static string Descripcion(MigrationStage stage) => stage switch
    {
        MigrationStage.ValidacionProyecto => "Validación del proyecto base",
        MigrationStage.CopiaExperimental => "Creación de copia experimental",
        MigrationStage.ActualizacionFramework => "Actualización del framework objetivo a .NET 10",
        MigrationStage.Restauracion => "Restauración de dependencias",
        _ => "Build preliminar .NET 10"
    };
}
