using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado e interacción de M-02 (CU-01). Seleccionar archivo, Validar ruta y Guardar proyecto
/// son acciones independientes: cambiar la ruta descarta la inspección anterior.
/// </summary>
public sealed class IncorporarProyectoFormulario
{
    private readonly IIncorporarProyectoService _servicio;
    private readonly IProjectFilePicker _selector;
    private readonly ILogger _logger;
    private string? _ruta;

    public IncorporarProyectoFormulario(IIncorporarProyectoService servicio, IProjectFilePicker selector, ILogger logger)
    {
        _servicio = servicio;
        _selector = selector;
        _logger = logger;
    }

    public string? Nombre { get; set; }

    /// <summary>Ruta del <c>.csproj</c>; si cambia se descarta la inspección anterior.</summary>
    public string? Ruta
    {
        get => _ruta;
        set
        {
            if (!string.Equals(_ruta, value, StringComparison.Ordinal))
            {
                _ruta = value;
                DescartarInspeccion();
            }
        }
    }

    public ProjectInspectionResult? Inspeccion { get; private set; }
    public bool Procesando { get; private set; }
    public bool SeleccionandoArchivo { get; private set; }
    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    public bool PuedeGuardar => !Procesando && Inspeccion is { EsValido: true } && !string.IsNullOrWhiteSpace(Nombre);

    public async Task SeleccionarArchivoAsync()
    {
        if (SeleccionandoArchivo)
        {
            return;
        }

        SeleccionandoArchivo = true;
        try
        {
            var resultado = await _selector.PickProjectFileAsync();
            switch (resultado.Status)
            {
                case ProjectFilePickerStatus.Selected:
                    _ruta = resultado.Path;
                    Inspeccion = null;
                    Mensaje = null;
                    break;
                case ProjectFilePickerStatus.Cancelled:
                    break;
                case ProjectFilePickerStatus.Unsupported:
                    MostrarMensaje(OperationMessageKind.Warning, "El selector de archivos solo está disponible cuando la herramienta se ejecuta en Windows. Escriba la ruta del archivo .csproj manualmente.");
                    break;
                case ProjectFilePickerStatus.Busy:
                    MostrarMensaje(OperationMessageKind.Info, "Ya hay un selector de archivos abierto. Termine o cancele esa selección.");
                    break;
                default:
                    MostrarMensaje(OperationMessageKind.Error, "No fue posible abrir el selector de archivos. Escriba la ruta del archivo .csproj manualmente.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al abrir el selector de archivos.");
            MostrarMensaje(OperationMessageKind.Error, "No fue posible abrir el selector de archivos. Escriba la ruta del archivo .csproj manualmente.");
        }
        finally
        {
            SeleccionandoArchivo = false;
        }
    }

    public async Task ValidarAsync()
    {
        Procesando = true;
        Mensaje = null;
        try
        {
            Inspeccion = await _servicio.InspeccionarAsync(_ruta);

            if (Inspeccion.EsValido && string.IsNullOrWhiteSpace(Nombre))
            {
                Nombre = Inspeccion.NombreSugerido;
            }

            MostrarMensaje(Inspeccion.EsValido ? OperationMessageKind.Info : OperationMessageKind.Error, Inspeccion.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al inspeccionar el proyecto.");
            Inspeccion = null;
            MostrarMensaje(OperationMessageKind.Error, "Ocurrió un error inesperado al validar el proyecto.");
        }
        finally
        {
            Procesando = false;
        }
    }

    public async Task GuardarAsync()
    {
        if (!PuedeGuardar)
        {
            return;
        }

        Procesando = true;
        Mensaje = null;
        try
        {
            var resultado = await _servicio.RegistrarAsync(new IncorporarProyectoRequest(Nombre, _ruta));
            if (resultado.Exito)
            {
                LimpiarFormulario();
                MostrarMensaje(OperationMessageKind.Success, resultado.Mensaje);
            }
            else
            {
                MostrarMensaje(OperationMessageKind.Error, resultado.Mensaje);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar el proyecto.");
            MostrarMensaje(OperationMessageKind.Error, "Ocurrió un error inesperado al registrar el proyecto.");
        }
        finally
        {
            Procesando = false;
        }
    }

    public void Cancelar()
    {
        LimpiarFormulario();
        Mensaje = null;
    }

    private void DescartarInspeccion()
    {
        if (Inspeccion is not null)
        {
            Inspeccion = null;
            Mensaje = null;
        }
    }

    private void LimpiarFormulario()
    {
        Nombre = null;
        _ruta = null;
        Inspeccion = null;
    }

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }
}
