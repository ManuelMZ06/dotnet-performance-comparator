using DotNetPerformanceComparator.Application.Evaluations.Configuration;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class ConfiguracionFormularioTests
{
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
            [new CatalogoItem(1, "Carga"), new CatalogoItem(2, "Estrés"), new CatalogoItem(3, "Concurrencia")],
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

    private static void Completar(ConfiguracionFormulario f)
    {
        f.Nombre = "Evaluación 01";
        f.ProyectoId = 1;
        f.MigracionId = 10;
        f.NumeroEjecuciones = 3;
        f.AlternarTipoPrueba(1);
        f.AlternarMetrica(1);
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
    public async Task MigracionesDelProyecto_SoloLasDelProyectoSeleccionado()
    {
        var (f, _) = await CrearAsync();

        f.ProyectoId = 1;
        Assert.Equal(new[] { 10, 11 }, f.MigracionesDelProyecto.Select(m => m.Id));

        f.ProyectoId = 2;
        Assert.Equal(new[] { 20 }, f.MigracionesDelProyecto.Select(m => m.Id));
    }

    [Fact]
    public async Task ToggleTiposYMetricas_PermiteSeleccionMultiple()
    {
        var (f, _) = await CrearAsync();

        f.AlternarTipoPrueba(1);
        f.AlternarTipoPrueba(2);
        f.AlternarMetrica(1);
        f.AlternarTipoPrueba(1); // vuelve a alternar: se quita

        Assert.Equal(new[] { 2 }, f.TipoPruebaIds.Order());
        Assert.Equal(new[] { 1 }, f.MetricaIds.Order());
    }

    [Fact]
    public async Task GuardarCorrecto_MarcaGuardadoYEnviaSeleccion()
    {
        var (f, s) = await CrearAsync();
        Completar(f);
        f.AlternarTipoPrueba(2);
        f.AlternarMetrica(2);

        await f.GuardarAsync();

        Assert.True(f.EstaGuardado);
        Assert.False(f.PuedeGuardar);
        Assert.Equal(55, f.ConfiguracionGuardadaId);
        Assert.Equal(OperationMessageKind.Success, f.MensajeTipo);
        Assert.Equal(new[] { 1, 2 }, s.UltimoRequest!.TipoPruebaIds.Order());
        Assert.Equal(new[] { 1, 2 }, s.UltimoRequest.MetricaIds.Order());
    }

    [Fact]
    public async Task ModificarDespuesDeGuardar_VuelveAHabilitarGuardado()
    {
        var (f, _) = await CrearAsync();
        Completar(f);
        await f.GuardarAsync();
        Assert.True(f.EstaGuardado);

        f.NumeroEjecuciones = 5;

        Assert.False(f.EstaGuardado);
        Assert.True(f.PuedeGuardar);
    }

    [Fact]
    public async Task GuardarDosVecesSinCambios_SoloLlamaUnaVez()
    {
        var (f, s) = await CrearAsync();
        Completar(f);

        await f.GuardarAsync();
        await f.GuardarAsync();

        Assert.Equal(1, s.Llamadas);
    }

    [Fact]
    public async Task ProyectoSinMigracionExitosa_BloqueaGuardado()
    {
        var servicio = new FakeServicio
        {
            Datos = new EvaluacionConfigurationData(
                [new ProyectoOpcion(3, "Sin migración")], [],
                [new CatalogoItem(1, "Carga")], [new CatalogoItem(1, "CPU")])
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
