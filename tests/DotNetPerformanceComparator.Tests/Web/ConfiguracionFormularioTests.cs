using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class ConfiguracionFormularioTests
{
    private const int Carga = 1, Estres = 2, Concurrencia = 3;

    private sealed class FakeServicio : IConfigurarEvaluacionService
    {
        public bool DevolverExito { get; set; } = true;
        public int Llamadas { get; private set; }
        public ConfigurarEvaluacionRequest? UltimoRequest { get; private set; }

        public EvaluacionConfigurationData Datos { get; set; } = new(
            [new ProyectoOpcion(1, "App A"), new ProyectoOpcion(2, "App B")],
            [
                new MigracionOpcion(10, 1, DateTime.UtcNow, @"C:\A10"),
                new MigracionOpcion(11, 1, DateTime.UtcNow, @"C:\A10b"),
                new MigracionOpcion(20, 2, DateTime.UtcNow, @"C:\B10")
            ],
            [new CatalogoItem(Carga, "Carga"), new CatalogoItem(Estres, "Estrés"), new CatalogoItem(Concurrencia, "Concurrencia")],
            [new CatalogoItem(1, "CPU", "%"), new CatalogoItem(2, "RAM", "MB")]);

        public Task<EvaluacionConfigurationData> ObtenerDatosAsync(CancellationToken ct = default) => Task.FromResult(Datos);

        public Task<ConfigurarEvaluacionResult> GuardarAsync(ConfigurarEvaluacionRequest request, CancellationToken ct = default)
        {
            Llamadas++;
            UltimoRequest = request;
            return Task.FromResult(DevolverExito
                ? new ConfigurarEvaluacionResult(true, "Guardada.", 55)
                : ConfigurarEvaluacionResult.Fallo("Error de validación."));
        }
    }

    private static async Task<(ConfiguracionFormulario F, FakeServicio S)> CrearAsync(FakeServicio? servicio = null)
    {
        servicio ??= new FakeServicio();
        var f = new ConfiguracionFormulario(servicio, NullLogger.Instance);
        await f.CargarAsync();
        return (f, servicio);
    }

    private static void CompletarGenerales(ConfiguracionFormulario f)
    {
        f.Nombre = "Evaluación 01";
        f.ProyectoId = 1;
        f.MigracionId = 10;
        f.NumeroEjecuciones = 3;
        f.RutaObjetivo = "/api/x";
        f.AlternarMetrica(1);
    }

    private static void LlenarCarga(ConfiguracionFormulario f)
    {
        f.Carga.UsuariosVirtuales = "50";
        f.Carga.DuracionSegundos = "120";
        f.Carga.WarmUpSegundos = "1";
        f.Carga.RampUpSegundos = "10";
        f.Carga.IntervaloSolicitudesMs = "1";
    }

    [Fact]
    public async Task SeleccionarCarga_MuestraSusParametros()
    {
        var (f, _) = await CrearAsync();
        Assert.False(f.MuestraCarga);

        f.AlternarTipoPrueba(Carga);

        Assert.True(f.MuestraCarga);
        Assert.False(f.MuestraEstres);
        Assert.False(f.MuestraConcurrencia);
    }

    [Fact]
    public async Task SeleccionarCadaTipo_UsaSuModeloIndependiente()
    {
        var (f, _) = await CrearAsync();
        f.AlternarTipoPrueba(Estres);
        Assert.True(f.MuestraEstres);
        f.AlternarTipoPrueba(Concurrencia);
        Assert.True(f.MuestraConcurrencia);

        f.Estres.UsuariosIniciales = "10";
        f.Concurrencia.Rondas = "5";

        Assert.Equal("10", f.Estres.UsuariosIniciales);
        Assert.Equal("5", f.Concurrencia.Rondas);
    }

    [Fact]
    public async Task DesmarcarCarga_NoPersisteSusParametros()
    {
        var (f, s) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        f.AlternarTipoPrueba(Carga); // se desmarca

        await f.GuardarAsync();

        Assert.Null(s.UltimoRequest!.Carga);
        Assert.Empty(s.UltimoRequest.TipoPruebaIds);
    }

    [Fact]
    public async Task Guardar_EnviaSoloParametrosDeTiposSeleccionados()
    {
        var (f, s) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        f.Estres.UsuariosIniciales = "99"; // Estrés NO seleccionado

        await f.GuardarAsync();

        Assert.NotNull(s.UltimoRequest!.Carga);
        Assert.Null(s.UltimoRequest.Estres);
        Assert.Equal("50", s.UltimoRequest.Carga!.UsuariosVirtuales);
        Assert.Equal(new[] { Carga }, s.UltimoRequest.TipoPruebaIds);
    }

    [Fact]
    public async Task NumeroEjecucionesYEndpoint_SeEnvian()
    {
        var (f, s) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        f.NumeroEjecuciones = 2;

        await f.GuardarAsync();

        Assert.Equal(2, s.UltimoRequest!.NumeroEjecuciones);
        Assert.Equal("/api/x", s.UltimoRequest.RutaObjetivo);
    }

    [Fact]
    public async Task CambiarProyecto_LimpiaMigracionId()
    {
        var (f, _) = await CrearAsync();
        f.ProyectoId = 1;
        f.MigracionId = 10;
        f.ProyectoId = 2;
        Assert.Null(f.MigracionId);
    }

    [Fact]
    public async Task GuardarCorrecto_MarcaGuardado()
    {
        var (f, _) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);

        await f.GuardarAsync();

        Assert.True(f.EstaGuardado);
        Assert.False(f.PuedeGuardar);
        Assert.Equal(OperationMessageKind.Success, f.MensajeTipo);
    }

    [Fact]
    public async Task GuardarDosVecesSinCambios_SoloLlamaUnaVez()
    {
        var (f, s) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);

        await f.GuardarAsync();
        await f.GuardarAsync();

        Assert.Equal(1, s.Llamadas);
    }

    [Fact]
    public async Task ModificarEndpointDespuesDeGuardar_VuelveAHabilitar()
    {
        var (f, _) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        await f.GuardarAsync();
        Assert.True(f.EstaGuardado);

        f.RutaObjetivo = "/otro";

        Assert.False(f.EstaGuardado);
        Assert.True(f.PuedeGuardar);
    }

    [Fact]
    public async Task ModificarObservacionesDespuesDeGuardar_VuelveAHabilitar()
    {
        var (f, _) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        await f.GuardarAsync();

        f.Observaciones = "nueva nota";

        Assert.False(f.EstaGuardado);
    }

    [Fact]
    public async Task ModificarParametroDeCargaDespuesDeGuardar_VuelveAHabilitar()
    {
        var (f, _) = await CrearAsync();
        CompletarGenerales(f);
        f.AlternarTipoPrueba(Carga);
        LlenarCarga(f);
        await f.GuardarAsync();

        f.Carga.UsuariosVirtuales = "80";

        Assert.False(f.EstaGuardado);
    }

    [Fact]
    public async Task ProyectoSinMigracionExitosa_BloqueaGuardado()
    {
        var servicio = new FakeServicio
        {
            Datos = new EvaluacionConfigurationData(
                [new ProyectoOpcion(3, "Sin migración")], [],
                [new CatalogoItem(Carga, "Carga")], [new CatalogoItem(1, "CPU")])
        };
        var (f, _) = await CrearAsync(servicio);
        f.ProyectoId = 3;
        Assert.True(f.ProyectoSinMigraciones);
        Assert.False(f.PuedeGuardar);
    }

    [Fact]
    public async Task SinProyectos_NoPermiteGuardar()
    {
        var servicio = new FakeServicio { Datos = new EvaluacionConfigurationData([], [], [], []) };
        var (f, _) = await CrearAsync(servicio);
        Assert.False(f.HayProyectos);
        Assert.False(f.PuedeGuardar);
        Assert.Equal(OperationMessageKind.Info, f.MensajeTipo);
    }
}
