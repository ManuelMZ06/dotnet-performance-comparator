using System.Globalization;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Reconstruye el estado persistido de una configuración (encabezado, ejecuciones, resultados y niveles de
/// estrés) para que M-05 pueda mostrarlo tras una recarga/reconexión, sin exponer EF Core a la capa Web.
/// </summary>
public sealed class ExecutionStatusQueryService : IExecutionStatusQueryService
{
    private readonly IEvaluationExecutionRepository _repo;

    public ExecutionStatusQueryService(IEvaluationExecutionRepository repo)
    {
        _repo = repo;
    }

    public async Task<EstadoEjecucionReconstruido?> ObtenerEstadoAsync(int configuracionId, CancellationToken cancellationToken = default)
    {
        var config = await _repo.GetConfiguracionConEjecucionesAsync(configuracionId, cancellationToken);
        if (config is null)
        {
            return null;
        }

        var metricas = config.ConfiguracionesMetrica.Select(cm => cm.Metrica.Nombre).ToList();
        var info = new ConfiguracionEjecutableInfo(
            config.Id, config.Nombre, config.Proyecto.Nombre, config.TipoPrueba.Nombre, config.RutaObjetivo,
            config.NumeroEjecuciones, config.Estado, metricas, PuedeEjecutar: false, MotivoNoEjecutable: null);

        var ejecuciones = config.Ejecuciones
            .OrderBy(e => OrdenGlobal(e))
            .ThenBy(e => e.Id)
            .Select(MapEjecucion)
            .ToList();

        var niveles = config.Ejecuciones
            .SelectMany(e => e.Niveles
                .OrderBy(n => n.NumeroNivel)
                .Select(n => MapNivel(e, n)))
            .ToList();

        return new EstadoEjecucionReconstruido(info, ejecuciones, niveles);
    }

    private static EjecucionResumen MapEjecucion(Ejecucion e)
    {
        decimal? Valor(string nombre) => e.Resultados.FirstOrDefault(r => r.Metrica.Nombre == nombre)?.Valor;
        return new EjecucionResumen(
            e.VersionDotNet, e.NumeroEjecucion, (int)OrdenGlobal(e), e.Estado,
            Valor(NombresMetrica.Cpu), Valor(NombresMetrica.MemoriaRam), Valor(NombresMetrica.TiempoRespuesta),
            Valor(NombresMetrica.Latencia), Valor(NombresMetrica.Throughput), Valor(NombresMetrica.TamanoAplicacion),
            Condicion(e, ClavesCondicionEjecucion.SolicitudesExitosas), Condicion(e, ClavesCondicionEjecucion.SolicitudesFallidas));
    }

    private static NivelEstresResumen MapNivel(Ejecucion e, NivelEstres n)
    {
        decimal? Valor(string nombre) => n.Resultados.FirstOrDefault(r => r.Metrica.Nombre == nombre)?.Valor;
        return new NivelEstresResumen(
            e.VersionDotNet, e.NumeroEjecucion, n.NumeroNivel, n.UsuariosObjetivo, n.Estado,
            Valor(NombresMetrica.Cpu), Valor(NombresMetrica.MemoriaRam), Valor(NombresMetrica.TiempoRespuesta),
            Valor(NombresMetrica.Latencia), Valor(NombresMetrica.Throughput),
            n.SolicitudesExitosas, n.SolicitudesFallidas);
    }

    private static long OrdenGlobal(Ejecucion e) =>
        long.TryParse(e.Condiciones.FirstOrDefault(c => c.Clave == ClavesCondicionEjecucion.OrdenGlobal)?.Valor,
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var orden) ? orden : e.Id;

    private static long Condicion(Ejecucion e, string clave) =>
        long.TryParse(e.Condiciones.FirstOrDefault(c => c.Clave == clave)?.Valor,
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : 0;
}
