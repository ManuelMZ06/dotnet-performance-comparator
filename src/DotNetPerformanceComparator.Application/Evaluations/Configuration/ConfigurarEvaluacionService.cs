using System.Globalization;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

public sealed class ConfigurarEvaluacionService : IConfigurarEvaluacionService
{
    private readonly IEvaluationCatalogRepository _catalogo;
    private readonly IProyectoDotNetRepository _proyectos;
    private readonly IConfiguracionEvaluacionRepository _configuraciones;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConfigurarEvaluacionService> _logger;

    public ConfigurarEvaluacionService(
        IEvaluationCatalogRepository catalogo,
        IProyectoDotNetRepository proyectos,
        IConfiguracionEvaluacionRepository configuraciones,
        TimeProvider timeProvider,
        ILogger<ConfigurarEvaluacionService> logger)
    {
        _catalogo = catalogo;
        _proyectos = proyectos;
        _configuraciones = configuraciones;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<EvaluacionConfigurationData> ObtenerDatosAsync(CancellationToken cancellationToken = default)
    {
        var proyectos = await _catalogo.GetProyectosAsync(cancellationToken);
        var migraciones = await _catalogo.GetMigracionesExitosasAsync(cancellationToken);
        var tipos = await _catalogo.GetTiposPruebaAsync(cancellationToken);
        var metricas = await _catalogo.GetMetricasAsync(cancellationToken);

        return new EvaluacionConfigurationData(
            proyectos.Select(p => new ProyectoOpcion(p.Id, p.Nombre)).ToList(),
            migraciones.Select(m => new MigracionOpcion(m.Id, m.ProyectoId, m.FechaFin ?? m.FechaInicio, m.RutaMigrada)).ToList(),
            tipos.Select(t => new CatalogoItem(t.Id, t.Nombre)).ToList(),
            metricas.Select(m => new CatalogoItem(m.Id, m.Nombre, m.Unidad)).ToList());
    }

    public async Task<ConfigurarEvaluacionResult> GuardarAsync(ConfigurarEvaluacionRequest request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre?.Trim();
        if (string.IsNullOrEmpty(nombre))
        {
            return ConfigurarEvaluacionResult.Fallo("Indique un nombre para la evaluación.");
        }

        if (request.NumeroEjecuciones is not { } ejecuciones || ejecuciones <= 0)
        {
            return ConfigurarEvaluacionResult.Fallo("El número de ejecuciones debe ser un entero mayor que cero.");
        }

        if (request.TipoPruebaIds.Count == 0)
        {
            return ConfigurarEvaluacionResult.Fallo("Seleccione al menos un tipo de prueba.");
        }

        if (request.MetricaIds.Count == 0)
        {
            return ConfigurarEvaluacionResult.Fallo("Seleccione al menos una métrica.");
        }

        if (request.ProyectoId is not { } proyectoId || await _proyectos.GetByIdAsync(proyectoId, cancellationToken) is null)
        {
            return ConfigurarEvaluacionResult.Fallo("El proyecto seleccionado no existe.");
        }

        if (request.MigracionId is not { } migracionId)
        {
            return ConfigurarEvaluacionResult.Fallo("Seleccione una migración .NET 10 exitosa.");
        }

        var migracion = await _catalogo.GetMigracionByIdAsync(migracionId, cancellationToken);
        if (migracion is null || migracion.ProyectoId != proyectoId)
        {
            return ConfigurarEvaluacionResult.Fallo("La migración seleccionada no pertenece al proyecto indicado.");
        }

        if (migracion.Estado != EstadosMigracion.Exitosa)
        {
            return ConfigurarEvaluacionResult.Fallo("La migración seleccionada no es una migración .NET 10 exitosa.");
        }

        var tiposValidos = (await _catalogo.GetTiposPruebaAsync(cancellationToken)).Select(t => t.Id).ToHashSet();
        var tiposPedidos = request.TipoPruebaIds.Distinct().ToList();
        if (!tiposPedidos.All(tiposValidos.Contains))
        {
            return ConfigurarEvaluacionResult.Fallo("Uno o más tipos de prueba seleccionados no son válidos.");
        }

        var metricasValidas = (await _catalogo.GetMetricasAsync(cancellationToken)).Select(m => m.Id).ToHashSet();
        var metricasPedidas = request.MetricaIds.Distinct().ToList();
        if (!metricasPedidas.All(metricasValidas.Contains))
        {
            return ConfigurarEvaluacionResult.Fallo("Una o más métricas seleccionadas no son válidas.");
        }

        if (!TryConstruirParametros(request.Parametros, out var parametros, out var errorParametro))
        {
            return ConfigurarEvaluacionResult.Fallo(errorParametro!);
        }

        var configuracion = new ConfiguracionEvaluacion
        {
            ProyectoId = proyectoId,
            MigracionId = migracionId,
            Nombre = nombre,
            NumeroEjecuciones = ejecuciones,
            Estado = EstadosConfiguracionEvaluacion.Configurada,
            FechaCreacion = _timeProvider.GetUtcNow().UtcDateTime,
            ConfiguracionesPrueba = tiposPedidos.Select(id => new ConfiguracionPrueba { TipoPruebaId = id }).ToList(),
            ConfiguracionesMetrica = metricasPedidas.Select(id => new ConfiguracionMetrica { MetricaId = id }).ToList(),
            Parametros = parametros
        };

        try
        {
            await _configuraciones.AddAsync(configuracion, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al guardar la configuración de evaluación del proyecto {ProyectoId}.", proyectoId);
            return ConfigurarEvaluacionResult.Fallo("No fue posible guardar la configuración. Inténtelo nuevamente.");
        }

        return new ConfigurarEvaluacionResult(true, $"La configuración de evaluación \"{nombre}\" se guardó correctamente.", configuracion.Id);
    }

    /// <summary>
    /// Parsea y valida los parámetros específicos. Solo se crean filas para los parámetros con valor.
    /// </summary>
    private static bool TryConstruirParametros(
        ParametrosEvaluacionInput input,
        out List<ParametroEvaluacion> parametros,
        out string? error)
    {
        parametros = [];
        error = null;

        (string Clave, string? Valor, bool PermiteCero, string Etiqueta)[] numericos =
        [
            (ClavesParametroEvaluacion.WarmUp, input.WarmUp, true, "Warm-up"),
            (ClavesParametroEvaluacion.UsuariosVirtuales, input.UsuariosVirtuales, false, "Usuarios virtuales"),
            (ClavesParametroEvaluacion.DuracionSegundos, input.DuracionSegundos, false, "Duración"),
            (ClavesParametroEvaluacion.RampUpSegundos, input.RampUpSegundos, true, "Ramp-up"),
            (ClavesParametroEvaluacion.Intervalo, input.Intervalo, false, "Intervalo")
        ];

        foreach (var (clave, valor, permiteCero, etiqueta) in numericos)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                continue;
            }

            if (!int.TryParse(valor.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
            {
                error = $"{etiqueta} debe ser un número entero.";
                return false;
            }

            if (permiteCero ? numero < 0 : numero <= 0)
            {
                error = permiteCero
                    ? $"{etiqueta} no puede ser negativo."
                    : $"{etiqueta} debe ser mayor que cero.";
                return false;
            }

            parametros.Add(new ParametroEvaluacion { Clave = clave, Valor = numero.ToString(CultureInfo.InvariantCulture) });
        }

        var observaciones = input.Observaciones?.Trim();
        if (!string.IsNullOrEmpty(observaciones))
        {
            parametros.Add(new ParametroEvaluacion { Clave = ClavesParametroEvaluacion.Observaciones, Valor = observaciones });
        }

        return true;
    }
}
