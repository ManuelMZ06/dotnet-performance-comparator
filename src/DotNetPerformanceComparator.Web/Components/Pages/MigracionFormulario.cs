using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado e interacción de M-03 (CU-02). Seleccionar carpeta solo rellena el directorio de salida;
/// la migración comienza únicamente con "Iniciar migración experimental".
/// </summary>
public sealed class MigracionFormulario
{
    private const int MaxLineasBitacora = 2000;

    private readonly IMigrarProyectoService _servicio;
    private readonly IDirectoryPicker _selector;
    private readonly ILogger _logger;
    private readonly Func<Action, Task> _despachar;
    private readonly Dictionary<MigrationStage, MigrationStageStatus> _etapas = [];
    private readonly List<string> _bitacora = [];

    /// <param name="despachar">Ejecuta una actualización del estado en el contexto de la interfaz y la refresca.</param>
    public MigracionFormulario(IMigrarProyectoService servicio, IDirectoryPicker selector, ILogger logger, Func<Action, Task> despachar)
    {
        _servicio = servicio;
        _selector = selector;
        _logger = logger;
        _despachar = despachar;
        ReiniciarEtapas();
    }

    public IReadOnlyList<ProyectoResumen> Proyectos { get; private set; } = [];
    public int? ProyectoId { get; set; }
    public string? DirectorioSalida { get; set; }
    public bool Ejecutando { get; private set; }
    public bool SeleccionandoCarpeta { get; private set; }
    public IReadOnlyDictionary<MigrationStage, MigrationStageStatus> Etapas => _etapas;
    public IReadOnlyList<string> Bitacora => _bitacora;
    public MigrarProyectoResult? Resultado { get; private set; }
    public bool MostrarDetalle { get; private set; }
    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    public ProyectoResumen? ProyectoSeleccionado => Proyectos.FirstOrDefault(p => p.Id == ProyectoId);

    public bool PuedeIniciar => !Ejecutando && ProyectoSeleccionado is not null && !string.IsNullOrWhiteSpace(DirectorioSalida);

    public async Task CargarAsync()
    {
        try
        {
            Proyectos = await _servicio.ObtenerProyectosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar los proyectos registrados.");
            MostrarMensaje(OperationMessageKind.Error, "No fue posible cargar los proyectos registrados.");
        }
    }

    public async Task SeleccionarCarpetaAsync()
    {
        if (SeleccionandoCarpeta || Ejecutando)
        {
            return;
        }

        SeleccionandoCarpeta = true;
        try
        {
            var resultado = await _selector.PickDirectoryAsync();
            switch (resultado.Status)
            {
                case DirectoryPickerStatus.Selected:
                    DirectorioSalida = resultado.Path;
                    break;
                case DirectoryPickerStatus.Cancelled:
                    break;
                case DirectoryPickerStatus.Unsupported:
                    MostrarMensaje(OperationMessageKind.Warning, "El selector de carpetas solo está disponible cuando la herramienta se ejecuta en Windows. Escriba el directorio de salida manualmente.");
                    break;
                case DirectoryPickerStatus.Busy:
                    MostrarMensaje(OperationMessageKind.Info, "Ya hay un selector abierto. Termine o cancele esa selección.");
                    break;
                default:
                    MostrarMensaje(OperationMessageKind.Error, "No fue posible abrir el selector de carpetas. Escriba el directorio de salida manualmente.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al abrir el selector de carpetas.");
            MostrarMensaje(OperationMessageKind.Error, "No fue posible abrir el selector de carpetas. Escriba el directorio de salida manualmente.");
        }
        finally
        {
            SeleccionandoCarpeta = false;
        }
    }

    public async Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        if (!PuedeIniciar)
        {
            return;
        }

        Ejecutando = true;
        ReiniciarEtapas();
        _bitacora.Clear();
        Resultado = null;
        MostrarDetalle = false;
        Mensaje = null;

        try
        {
            Resultado = await _servicio.MigrarAsync(
                new MigrarProyectoRequest(ProyectoId, DirectorioSalida?.Trim()),
                new Progreso(this),
                cancellationToken);

            MostrarMensaje(Resultado.Exito ? OperationMessageKind.Success : OperationMessageKind.Error, Resultado.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante la migración experimental.");
            MostrarMensaje(OperationMessageKind.Error, "Ocurrió un error inesperado durante la migración experimental.");
        }
        finally
        {
            Ejecutando = false;
        }
    }

    public void AlternarDetalle()
    {
        if (Resultado?.Migracion is not null)
        {
            MostrarDetalle = !MostrarDetalle;
        }
    }

    private void Aplicar(MigrationProgress progreso)
    {
        if (progreso.Stage is { } etapa)
        {
            _etapas[etapa] = progreso.StageStatus;
        }
        else if (progreso.LogLine is not null)
        {
            if (_bitacora.Count >= MaxLineasBitacora)
            {
                _bitacora.RemoveAt(0);
            }

            _bitacora.Add(progreso.LogLine);
        }
    }

    private void ReiniciarEtapas()
    {
        foreach (var etapa in MigrationStages.All)
        {
            _etapas[etapa] = MigrationStageStatus.Pendiente;
        }
    }

    private void MostrarMensaje(OperationMessageKind tipo, string mensaje)
    {
        MensajeTipo = tipo;
        Mensaje = mensaje;
    }

    /// <summary>Recibe el progreso (posiblemente desde otros hilos) y lo aplica en el contexto de la interfaz.</summary>
    private sealed class Progreso(MigracionFormulario formulario) : IProgress<MigrationProgress>
    {
        public void Report(MigrationProgress value) => _ = formulario._despachar(() => formulario.Aplicar(value));
    }
}
