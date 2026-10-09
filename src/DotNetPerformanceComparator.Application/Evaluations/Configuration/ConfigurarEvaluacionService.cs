using System.Globalization;
using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DotNetPerformanceComparator.Application.Evaluations.Configuration;

public sealed class ConfigurarEvaluacionService : IConfigurarEvaluacionService
{
    public const int MinEjecuciones = 1;
    public const int MaxEjecuciones = 3;
    public const string MetodoHttp = "GET";

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

        if (request.NumeroEjecuciones is not { } ejecuciones || ejecuciones < MinEjecuciones || ejecuciones > MaxEjecuciones)
        {
            return ConfigurarEvaluacionResult.Fallo($"El número de ejecuciones debe estar entre {MinEjecuciones} y {MaxEjecuciones}.");
        }

        if (!TryValidarRuta(request.RutaObjetivo, out var ruta, out var errorRuta))
        {
            return ConfigurarEvaluacionResult.Fallo(errorRuta!);
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

        var tipos = await _catalogo.GetTiposPruebaAsync(cancellationToken);
        var tiposPorId = tipos.ToDictionary(t => t.Id);
        var tiposPedidos = request.TipoPruebaIds.Distinct().ToList();
        if (!tiposPedidos.All(tiposPorId.ContainsKey))
        {
            return ConfigurarEvaluacionResult.Fallo("Uno o más tipos de prueba seleccionados no son válidos.");
        }

        var metricasValidas = (await _catalogo.GetMetricasAsync(cancellationToken)).Select(m => m.Id).ToHashSet();
        var metricasPedidas = request.MetricaIds.Distinct().ToList();
        if (!metricasPedidas.All(metricasValidas.Contains))
        {
            return ConfigurarEvaluacionResult.Fallo("Una o más métricas seleccionadas no son válidas.");
        }

        var pruebas = new List<ConfiguracionPrueba>();
        foreach (var tipoId in tiposPedidos)
        {
            if (!TryConstruirParametros(tiposPorId[tipoId].Nombre, request, out var parametros, out var errorParametro))
            {
                return ConfigurarEvaluacionResult.Fallo(errorParametro!);
            }

            pruebas.Add(new ConfiguracionPrueba
            {
                TipoPruebaId = tipoId,
                Parametros = parametros.Select(p => new ParametroEvaluacion { TipoPruebaId = tipoId, Clave = p.Clave, Valor = p.Valor }).ToList()
            });
        }

        var configuracion = new ConfiguracionEvaluacion
        {
            ProyectoId = proyectoId,
            MigracionId = migracionId,
            Nombre = nombre,
            NumeroEjecuciones = ejecuciones,
            RutaObjetivo = ruta,
            Observaciones = NormalizarObservaciones(request.Observaciones),
            Estado = EstadosConfiguracionEvaluacion.Configurada,
            FechaCreacion = _timeProvider.GetUtcNow().UtcDateTime,
            ConfiguracionesPrueba = pruebas,
            ConfiguracionesMetrica = metricasPedidas.Select(id => new ConfiguracionMetrica { MetricaId = id }).ToList()
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

    private static string? NormalizarObservaciones(string? observaciones)
    {
        var texto = observaciones?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    /// <summary>La ruta debe ser relativa (comenzar con <c>/</c>), no una URL absoluta.</summary>
    private static bool TryValidarRuta(string? rutaObjetivo, out string ruta, out string? error)
    {
        ruta = string.Empty;
        error = null;

        var valor = rutaObjetivo?.Trim();
        if (string.IsNullOrEmpty(valor))
        {
            error = "Indique la ruta (endpoint) relativa que se probará, por ejemplo /api/productos.";
            return false;
        }

        if (valor.Contains("://", StringComparison.Ordinal) || Uri.TryCreate(valor, UriKind.Absolute, out _))
        {
            error = "La ruta debe ser relativa a la aplicación (no una URL absoluta). Ejemplo: /api/productos.";
            return false;
        }

        if (!valor.StartsWith('/'))
        {
            error = "La ruta debe comenzar con \"/\". Ejemplo: /api/productos.";
            return false;
        }

        ruta = valor;
        return true;
    }

    /// <summary>Construye y valida los parámetros del tipo de prueba indicado por su nombre de catálogo.</summary>
    private static bool TryConstruirParametros(
        string nombreTipo,
        ConfigurarEvaluacionRequest request,
        out List<(string Clave, string Valor)> parametros,
        out string? error)
    {
        parametros = [];
        error = null;

        return nombreTipo switch
        {
            TiposPruebaConocidos.Carga => ConstruirCarga(request.Carga, parametros, out error),
            TiposPruebaConocidos.Estres => ConstruirEstres(request.Estres, parametros, out error),
            TiposPruebaConocidos.Concurrencia => ConstruirConcurrencia(request.Concurrencia, parametros, out error),
            _ => Rechazar($"El tipo de prueba \"{nombreTipo}\" no está soportado.", out error)
        };
    }

    private static bool ConstruirCarga(ParametrosCargaInput? input, List<(string, string)> parametros, out string? error)
    {
        input ??= new ParametrosCargaInput();
        return Numerico(input.UsuariosVirtuales, false, "Carga: usuarios virtuales", ClavesParametroCarga.UsuariosVirtuales, parametros, out error)
            && Numerico(input.DuracionSegundos, false, "Carga: duración (s)", ClavesParametroCarga.DuracionSegundos, parametros, out error)
            && Numerico(input.WarmUpSegundos, true, "Carga: warm-up (s)", ClavesParametroCarga.WarmUpSegundos, parametros, out error)
            && Numerico(input.RampUpSegundos, true, "Carga: ramp-up (s)", ClavesParametroCarga.RampUpSegundos, parametros, out error)
            && Numerico(input.IntervaloSolicitudesMs, true, "Carga: intervalo entre solicitudes (ms)", ClavesParametroCarga.IntervaloSolicitudesMs, parametros, out error);
    }

    private static bool ConstruirEstres(ParametrosEstresInput? input, List<(string, string)> parametros, out string? error)
    {
        input ??= new ParametrosEstresInput();
        if (!Numerico(input.UsuariosIniciales, false, "Estrés: usuarios iniciales", ClavesParametroEstres.UsuariosIniciales, parametros, out error)
            || !Numerico(input.IncrementoUsuarios, false, "Estrés: incremento de usuarios", ClavesParametroEstres.IncrementoUsuarios, parametros, out error)
            || !Numerico(input.UsuariosMaximos, false, "Estrés: usuarios máximos", ClavesParametroEstres.UsuariosMaximos, parametros, out error)
            || !Numerico(input.DuracionNivelSegundos, false, "Estrés: duración por nivel (s)", ClavesParametroEstres.DuracionNivelSegundos, parametros, out error)
            || !Numerico(input.WarmUpSegundos, true, "Estrés: warm-up (s)", ClavesParametroEstres.WarmUpSegundos, parametros, out error)
            || !Numerico(input.IntervaloSolicitudesMs, true, "Estrés: intervalo entre solicitudes (ms)", ClavesParametroEstres.IntervaloSolicitudesMs, parametros, out error))
        {
            return false;
        }

        var iniciales = int.Parse(parametros.First(p => p.Item1 == ClavesParametroEstres.UsuariosIniciales).Item2, CultureInfo.InvariantCulture);
        var maximos = int.Parse(parametros.First(p => p.Item1 == ClavesParametroEstres.UsuariosMaximos).Item2, CultureInfo.InvariantCulture);
        if (maximos <= iniciales)
        {
            error = "Estrés: los usuarios máximos deben ser mayores que los usuarios iniciales.";
            return false;
        }

        return true;
    }

    private static bool ConstruirConcurrencia(ParametrosConcurrenciaInput? input, List<(string, string)> parametros, out string? error)
    {
        input ??= new ParametrosConcurrenciaInput();
        return Numerico(input.UsuariosConcurrentes, false, "Concurrencia: usuarios concurrentes", ClavesParametroConcurrencia.UsuariosConcurrentes, parametros, out error)
            && Numerico(input.Rondas, false, "Concurrencia: rondas", ClavesParametroConcurrencia.Rondas, parametros, out error)
            && Numerico(input.WarmUpSegundos, true, "Concurrencia: warm-up (s)", ClavesParametroConcurrencia.WarmUpSegundos, parametros, out error)
            && Numerico(input.IntervaloRondasMs, true, "Concurrencia: intervalo entre rondas (ms)", ClavesParametroConcurrencia.IntervaloRondasMs, parametros, out error);
    }

    /// <summary>Valida un parámetro numérico obligatorio y, si es válido, lo agrega a la lista.</summary>
    private static bool Numerico(string? valor, bool permiteCero, string etiqueta, string clave, List<(string, string)> parametros, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            error = $"{etiqueta} es obligatorio.";
            return false;
        }

        if (!int.TryParse(valor.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
        {
            error = $"{etiqueta} debe ser un número entero.";
            return false;
        }

        if (permiteCero ? numero < 0 : numero <= 0)
        {
            error = permiteCero ? $"{etiqueta} no puede ser negativo." : $"{etiqueta} debe ser mayor que cero.";
            return false;
        }

        parametros.Add((clave, numero.ToString(CultureInfo.InvariantCulture)));
        return true;
    }

    private static bool Rechazar(string mensaje, out string? error)
    {
        error = mensaje;
        return false;
    }
}
