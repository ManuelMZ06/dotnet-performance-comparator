namespace DotNetPerformanceComparator.Application.Evaluations.Results;

/// <summary>
/// CU-05 — Consultar resultados (RF-07). Servicio de **solo lectura** que expone el listado de evaluaciones
/// y el detalle de una evaluación con sus resultados persistidos. No edita, borra, ejecuta, cancela, filtra,
/// compara ni genera reportes (eso corresponde a CU-06/CU-07/CU-08/CU-09).
/// </summary>
public interface IConsultaResultadosService
{
    Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken cancellationToken = default);

    Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int configuracionId, CancellationToken cancellationToken = default);
}

public sealed class ConsultaResultadosService : IConsultaResultadosService
{
    private readonly IConsultaResultadosRepository _repositorio;

    public ConsultaResultadosService(IConsultaResultadosRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<IReadOnlyList<EvaluacionResumenConsulta>> ObtenerEvaluacionesAsync(CancellationToken cancellationToken = default) =>
        _repositorio.ListarAsync(cancellationToken);

    public Task<EvaluacionDetalleConsulta?> ObtenerDetalleAsync(int configuracionId, CancellationToken cancellationToken = default) =>
        _repositorio.ObtenerDetalleAsync(configuracionId, cancellationToken);
}
