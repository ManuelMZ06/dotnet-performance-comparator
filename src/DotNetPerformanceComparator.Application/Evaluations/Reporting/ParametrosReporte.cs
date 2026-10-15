using DotNetPerformanceComparator.Application.Evaluations.Results;

namespace DotNetPerformanceComparator.Application.Evaluations.Reporting;

/// <summary>
/// Formatea los parámetros del experimento (por tipo de prueba) como líneas etiqueta/valor para el reporte.
/// Lógica pura y única (reutilizable). Un parámetro ausente (<c>null</c>) se muestra como "No disponible",
/// nunca como 0: ausencia ≠ cero. Solo se incluyen los parámetros del tipo correspondiente.
/// </summary>
public static class ParametrosReporte
{
    public const string NoDisponible = "No disponible";

    public static IReadOnlyList<(string Etiqueta, string Valor)> Lineas(ParametrosPruebaConsulta? parametros) => parametros switch
    {
        ParametrosCargaConsulta p =>
        [
            ("Usuarios virtuales", Con(p.UsuariosVirtuales, "usuarios")),
            ("Duración", Con(p.DuracionSegundos, "s")),
            ("Warm-up", Con(p.WarmUpSegundos, "s")),
            ("Ramp-up", Con(p.RampUpSegundos, "s")),
            ("Intervalo entre solicitudes", Con(p.IntervaloSolicitudesMs, "ms"))
        ],
        ParametrosEstresConsulta p =>
        [
            ("Usuarios iniciales", Con(p.UsuariosIniciales, "usuarios")),
            ("Incremento por nivel", Con(p.IncrementoUsuarios, "usuarios")),
            ("Usuarios máximos", Con(p.UsuariosMaximos, "usuarios")),
            ("Duración por nivel", Con(p.DuracionNivelSegundos, "s")),
            ("Warm-up", Con(p.WarmUpSegundos, "s")),
            ("Intervalo entre solicitudes", Con(p.IntervaloSolicitudesMs, "ms"))
        ],
        ParametrosConcurrenciaConsulta p =>
        [
            ("Usuarios concurrentes", Con(p.UsuariosConcurrentes, "usuarios")),
            ("Rondas", Con(p.Rondas, "rondas")),
            ("Warm-up", Con(p.WarmUpSegundos, "s")),
            ("Intervalo entre rondas", Con(p.IntervaloRondasMs, "ms"))
        ],
        _ => []
    };

    private static string Con(int? valor, string unidad) => valor is { } v ? $"{v} {unidad}" : NoDisponible;
}
