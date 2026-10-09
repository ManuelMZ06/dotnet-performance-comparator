using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado e interacción de M-04 (CU-03). La validación definitiva la hace Application; aquí solo
/// se gestiona el estado del formulario (selección dependiente, selección múltiple, dirty/guardado).
/// Cada guardado correcto crea una nueva configuración; no hay edición del registro guardado.
/// </summary>
public sealed class ConfiguracionFormulario
{
    private readonly IConfigurarEvaluacionService _servicio;
    private readonly ILogger _logger;

    private string? _nombre;
    private int? _proyectoId;
    private int? _migracionId;
    private int? _numeroEjecuciones;
    private string? _warmUp;
    private string? _usuariosVirtuales;
    private string? _duracionSegundos;
    private string? _rampUpSegundos;
    private string? _intervalo;
    private string? _observaciones;

    public ConfiguracionFormulario(IConfigurarEvaluacionService servicio, ILogger logger)
    {
        _servicio = servicio;
        _logger = logger;
    }

    public EvaluacionConfigurationData Datos { get; private set; } =
        new([], [], [], []);

    public HashSet<int> TipoPruebaIds { get; } = [];
    public HashSet<int> MetricaIds { get; } = [];

    public bool Guardando { get; private set; }
    public bool EstaGuardado { get; private set; }
    public int? ConfiguracionGuardadaId { get; private set; }
    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    // Campos: cada cambio marca el formulario como editable (dirty) de nuevo.
    public string? Nombre { get => _nombre; set => Asignar(ref _nombre, value); }
    public int? NumeroEjecuciones { get => _numeroEjecuciones; set => Asignar(ref _numeroEjecuciones, value); }
    public string? WarmUp { get => _warmUp; set => Asignar(ref _warmUp, value); }
    public string? UsuariosVirtuales { get => _usuariosVirtuales; set => Asignar(ref _usuariosVirtuales, value); }
    public string? DuracionSegundos { get => _duracionSegundos; set => Asignar(ref _duracionSegundos, value); }
    public string? RampUpSegundos { get => _rampUpSegundos; set => Asignar(ref _rampUpSegundos, value); }
    public string? Intervalo { get => _intervalo; set => Asignar(ref _intervalo, value); }
    public string? Observaciones { get => _observaciones; set => Asignar(ref _observaciones, value); }

    public int? MigracionId { get => _migracionId; set => Asignar(ref _migracionId, value); }

    /// <summary>Al cambiar de proyecto se descarta la migración seleccionada (puede ser de otro proyecto).</summary>
    public int? ProyectoId
    {
        get => _proyectoId;
        set
        {
            if (_proyectoId != value)
            {
                _proyectoId = value;
                _migracionId = null;
                MarcarEditable();
            }
        }
    }

    public IReadOnlyList<MigracionOpcion> MigracionesDelProyecto =>
        _proyectoId is { } id
            ? Datos.MigracionesExitosas.Where(m => m.ProyectoId == id).ToList()
            : [];

    public bool HayProyectos => Datos.Proyectos.Count > 0;

    public bool ProyectoSinMigraciones => _proyectoId is not null && MigracionesDelProyecto.Count == 0;

    public bool PuedeGuardar => !Guardando && EstaEditable && HayProyectos && !ProyectoSinMigraciones;

    private bool EstaEditable => !EstaGuardado;

    public async Task CargarAsync()
    {
        try
        {
            Datos = await _servicio.ObtenerDatosAsync();
            if (!HayProyectos)
            {
                MostrarMensaje(OperationMessageKind.Info, "No hay proyectos registrados. Incorpore un proyecto y realice una migración .NET 10 exitosa antes de configurar una evaluación.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar los datos de configuración.");
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar los datos para configurar la evaluación.");
        }
    }

    public void AlternarTipoPrueba(int id) => Alternar(TipoPruebaIds, id);

    public void AlternarMetrica(int id) => Alternar(MetricaIds, id);

    public async Task GuardarAsync()
    {
        if (!PuedeGuardar)
        {
            return;
        }

        Guardando = true;
        Mensaje = null;
        try
        {
            var request = new ConfigurarEvaluacionRequest(
                _nombre,
                _proyectoId,
                _migracionId,
                _numeroEjecuciones,
                TipoPruebaIds.ToList(),
                MetricaIds.ToList(),
                new ParametrosEvaluacionInput(_warmUp, _usuariosVirtuales, _duracionSegundos, _rampUpSegundos, _intervalo, _observaciones));

            var resultado = await _servicio.GuardarAsync(request);
            if (resultado.Exito)
            {
                EstaGuardado = true;
                ConfiguracionGuardadaId = resultado.ConfiguracionId;
                MostrarMensaje(OperationMessageKind.Success, resultado.Mensaje);
            }
            else
            {
                MostrarMensaje(OperationMessageKind.Error, resultado.Mensaje);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al guardar la configuración de evaluación.");
            MostrarMensaje(OperationMessageKind.Error, "Ocurrió un error inesperado al guardar la configuración.");
        }
        finally
        {
            Guardando = false;
        }
    }

    private void Alternar(HashSet<int> conjunto, int id)
    {
        if (!conjunto.Remove(id))
        {
            conjunto.Add(id);
        }

        MarcarEditable();
    }

    private void Asignar<T>(ref T campo, T valor)
    {
        if (!EqualityComparer<T>.Default.Equals(campo, valor))
        {
            campo = valor;
            MarcarEditable();
        }
    }

    /// <summary>Tras guardar, cualquier cambio vuelve a habilitar el guardado como una nueva configuración.</summary>
    private void MarcarEditable()
    {
        if (EstaGuardado)
        {
            EstaGuardado = false;
            Mensaje = null;
        }
    }

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }
}
