namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Valores persistidos en <c>NivelEstres.Estado</c> (C-07.3). Un nivel es un escalón de la prueba de
/// Estrés (una cantidad fija de usuarios durante una ventana).
/// </summary>
public static class EstadosNivelEstres
{
    /// <summary>Nivel iniciado; aún midiéndose.</summary>
    public const string EnProceso = "EnProceso";

    /// <summary>NBomber ejecutó toda la ventana del nivel y hubo al menos una solicitud exitosa.</summary>
    public const string Completado = "Completado";

    /// <summary>
    /// Comportamiento de saturación de la aplicación bajo estrés (0 solicitudes exitosas en la ventana,
    /// o el proceso objetivo terminó durante el nivel). No es un fallo técnico del evaluador.
    /// </summary>
    public const string Saturado = "Saturado";

    /// <summary>Un problema técnico del evaluador impidió realizar válidamente el nivel.</summary>
    public const string Fallido = "Fallido";

    /// <summary>Cancelado por <see cref="System.Threading.CancellationToken"/>.</summary>
    public const string Cancelado = "Cancelado";
}
