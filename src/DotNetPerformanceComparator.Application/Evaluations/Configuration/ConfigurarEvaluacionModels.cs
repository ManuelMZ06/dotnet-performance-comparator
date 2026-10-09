namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

/// <summary>Parámetros de la prueba de Carga, tal como se escriben en M-04 (sin parsear).</summary>
public sealed record ParametrosCargaInput(
    string? UsuariosVirtuales = null,
    string? DuracionSegundos = null,
    string? WarmUpSegundos = null,
    string? RampUpSegundos = null,
    string? IntervaloSolicitudesMs = null);

/// <summary>Parámetros de la prueba de Estrés.</summary>
public sealed record ParametrosEstresInput(
    string? UsuariosIniciales = null,
    string? IncrementoUsuarios = null,
    string? UsuariosMaximos = null,
    string? DuracionNivelSegundos = null,
    string? WarmUpSegundos = null,
    string? IntervaloSolicitudesMs = null);

/// <summary>Parámetros de la prueba de Concurrencia.</summary>
public sealed record ParametrosConcurrenciaInput(
    string? UsuariosConcurrentes = null,
    string? Rondas = null,
    string? WarmUpSegundos = null,
    string? IntervaloRondasMs = null);

/// <summary>
/// Datos para configurar una evaluación. Los parámetros van en modelos tipados por tipo de prueba;
/// solo se consideran los correspondientes a los tipos realmente seleccionados.
/// </summary>
public sealed record ConfigurarEvaluacionRequest(
    string? Nombre,
    int? ProyectoId,
    int? MigracionId,
    int? NumeroEjecuciones,
    string? RutaObjetivo,
    string? Observaciones,
    IReadOnlyList<int> TipoPruebaIds,
    IReadOnlyList<int> MetricaIds,
    ParametrosCargaInput? Carga = null,
    ParametrosEstresInput? Estres = null,
    ParametrosConcurrenciaInput? Concurrencia = null);

public sealed record ConfigurarEvaluacionResult(bool Exito, string Mensaje, int? ConfiguracionId = null)
{
    public static ConfigurarEvaluacionResult Fallo(string mensaje) => new(false, mensaje);
}
