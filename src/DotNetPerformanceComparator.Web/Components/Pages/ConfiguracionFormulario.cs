using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Domain.Constants;
using DotNetPerformanceComparator.Web.Components.Shared;

namespace DotNetPerformanceComparator.Web.Components.Pages;

/// <summary>
/// Estado e interacción de M-04 (CU-03). La validación definitiva la hace Application; aquí solo
/// se gestiona el estado del formulario. Cada tipo de prueba tiene su propio conjunto de parámetros;
/// solo se envían los de los tipos seleccionados. Cada guardado correcto crea una nueva configuración.
/// </summary>
public sealed class ConfiguracionFormulario
{
    private readonly IConfigurarEvaluacionService _servicio;
    private readonly ILogger _logger;

    private string? _nombre;
    private int? _proyectoId;
    private int? _migracionId;
    private int? _tipoPruebaId;
    private int? _numeroEjecuciones;
    private string? _rutaObjetivo;
    private string? _observaciones;

    public ConfiguracionFormulario(IConfigurarEvaluacionService servicio, ILogger logger)
    {
        _servicio = servicio;
        _logger = logger;
        Carga = new ParametrosCargaForm(MarcarEditable);
        Estres = new ParametrosEstresForm(MarcarEditable);
        Concurrencia = new ParametrosConcurrenciaForm(MarcarEditable);
    }

    public const string MetodoHttp = ConfigurarEvaluacionService.MetodoHttp;
    public const int MinEjecuciones = ConfigurarEvaluacionService.MinEjecuciones;
    public const int MaxEjecuciones = ConfigurarEvaluacionService.MaxEjecuciones;

    public EvaluacionConfigurationData Datos { get; private set; } = new([], [], [], []);

    public HashSet<int> MetricaIds { get; } = [];

    public ParametrosCargaForm Carga { get; }
    public ParametrosEstresForm Estres { get; }
    public ParametrosConcurrenciaForm Concurrencia { get; }

    public bool Guardando { get; private set; }
    public bool EstaGuardado { get; private set; }
    public int? ConfiguracionGuardadaId { get; private set; }
    public string? Mensaje { get; private set; }
    public OperationMessageKind MensajeTipo { get; private set; }

    public string? Nombre { get => _nombre; set => Asignar(ref _nombre, value); }
    public int? TipoPruebaId { get => _tipoPruebaId; set => Asignar(ref _tipoPruebaId, value); }
    public int? NumeroEjecuciones { get => _numeroEjecuciones; set => Asignar(ref _numeroEjecuciones, value); }
    public string? RutaObjetivo { get => _rutaObjetivo; set => Asignar(ref _rutaObjetivo, value); }
    public string? Observaciones { get => _observaciones; set => Asignar(ref _observaciones, value); }
    public int? MigracionId { get => _migracionId; set => Asignar(ref _migracionId, value); }

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
        _proyectoId is { } id ? Datos.MigracionesExitosas.Where(m => m.ProyectoId == id).ToList() : [];

    public bool HayProyectos => Datos.Proyectos.Count > 0;
    public bool ProyectoSinMigraciones => _proyectoId is not null && MigracionesDelProyecto.Count == 0;
    public bool PuedeGuardar => !Guardando && !EstaGuardado && HayProyectos && !ProyectoSinMigraciones;

    public int? CargaId => IdTipo(TiposPruebaConocidos.Carga);
    public int? EstresId => IdTipo(TiposPruebaConocidos.Estres);
    public int? ConcurrenciaId => IdTipo(TiposPruebaConocidos.Concurrencia);

    public bool MuestraCarga => _tipoPruebaId is { } id && id == CargaId;
    public bool MuestraEstres => _tipoPruebaId is { } id && id == EstresId;
    public bool MuestraConcurrencia => _tipoPruebaId is { } id && id == ConcurrenciaId;

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
                _nombre, _proyectoId, _migracionId, _tipoPruebaId, _numeroEjecuciones, _rutaObjetivo, _observaciones,
                MetricaIds.ToList(),
                MuestraCarga ? Carga.ToInput() : null,
                MuestraEstres ? Estres.ToInput() : null,
                MuestraConcurrencia ? Concurrencia.ToInput() : null);

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

    private int? IdTipo(string nombre) =>
        Datos.TiposPrueba.FirstOrDefault(t => t.Nombre == nombre)?.Id;

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

    /// <summary>Campo de texto que notifica cada cambio.</summary>
    public abstract class ParametrosForm(Action onChange)
    {
        private readonly Action _onChange = onChange;

        protected string? Set(ref string? campo, string? valor)
        {
            if (campo != valor)
            {
                campo = valor;
                _onChange();
            }

            return campo;
        }
    }

    public sealed class ParametrosCargaForm(Action onChange) : ParametrosForm(onChange)
    {
        private string? _uv, _dur, _warm, _ramp, _int;
        public string? UsuariosVirtuales { get => _uv; set => Set(ref _uv, value); }
        public string? DuracionSegundos { get => _dur; set => Set(ref _dur, value); }
        public string? WarmUpSegundos { get => _warm; set => Set(ref _warm, value); }
        public string? RampUpSegundos { get => _ramp; set => Set(ref _ramp, value); }
        public string? IntervaloSolicitudesMs { get => _int; set => Set(ref _int, value); }

        public ParametrosCargaInput ToInput() => new(UsuariosVirtuales, DuracionSegundos, WarmUpSegundos, RampUpSegundos, IntervaloSolicitudesMs);
    }

    public sealed class ParametrosEstresForm(Action onChange) : ParametrosForm(onChange)
    {
        private string? _ini, _inc, _max, _dur, _warm, _int;
        public string? UsuariosIniciales { get => _ini; set => Set(ref _ini, value); }
        public string? IncrementoUsuarios { get => _inc; set => Set(ref _inc, value); }
        public string? UsuariosMaximos { get => _max; set => Set(ref _max, value); }
        public string? DuracionNivelSegundos { get => _dur; set => Set(ref _dur, value); }
        public string? WarmUpSegundos { get => _warm; set => Set(ref _warm, value); }
        public string? IntervaloSolicitudesMs { get => _int; set => Set(ref _int, value); }

        public ParametrosEstresInput ToInput() => new(UsuariosIniciales, IncrementoUsuarios, UsuariosMaximos, DuracionNivelSegundos, WarmUpSegundos, IntervaloSolicitudesMs);
    }

    public sealed class ParametrosConcurrenciaForm(Action onChange) : ParametrosForm(onChange)
    {
        private string? _uc, _rondas, _warm, _int;
        public string? UsuariosConcurrentes { get => _uc; set => Set(ref _uc, value); }
        public string? Rondas { get => _rondas; set => Set(ref _rondas, value); }
        public string? WarmUpSegundos { get => _warm; set => Set(ref _warm, value); }
        public string? IntervaloRondasMs { get => _int; set => Set(ref _int, value); }

        public ParametrosConcurrenciaInput ToInput() => new(UsuariosConcurrentes, Rondas, WarmUpSegundos, IntervaloRondasMs);
    }
}
