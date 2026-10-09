namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

/// <summary>
/// CU-03 — Configurar evaluación (RF-03, RF-14). No ejecuta pruebas.
/// </summary>
public interface IConfigurarEvaluacionService
{
    /// <summary>Datos de catálogo para construir M-04.</summary>
    Task<EvaluacionConfigurationData> ObtenerDatosAsync(CancellationToken cancellationToken = default);

    /// <summary>Valida la configuración y, si es correcta, la persiste como agregado.</summary>
    Task<ConfigurarEvaluacionResult> GuardarAsync(ConfigurarEvaluacionRequest request, CancellationToken cancellationToken = default);
}
