using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class MigracionFormularioTests
{
    private sealed class FakeServicio : IMigrarProyectoService
    {
        public TaskCompletionSource<MigrarProyectoResult> Resultado { get; } = new();
        public IProgress<MigrationProgress>? Progreso { get; private set; }
        public int Llamadas { get; private set; }

        public Task<IReadOnlyList<ProyectoResumen>> ObtenerProyectosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProyectoResumen>>([new ProyectoResumen(1, "App", @"C:\App\App.csproj")]);

        public Task<MigrarProyectoResult> MigrarAsync(MigrarProyectoRequest request, IProgress<MigrationProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            Progreso = progress;
            return Resultado.Task;
        }
    }

    private sealed class FakeSelector(DirectoryPickerResult resultado) : IDirectoryPicker
    {
        public Task<DirectoryPickerResult> PickDirectoryAsync(CancellationToken cancellationToken = default) => Task.FromResult(resultado);
    }

    private static MigracionResumen Resumen(string estado) =>
        new(7, "App", ".NET 8", ".NET 10", "10.0.400", @"C:\Salida\App10", DateTime.UtcNow, DateTime.UtcNow, estado, null);

    private static async Task<(MigracionFormulario Formulario, FakeServicio Servicio)> CrearAsync(DirectoryPickerResult? seleccion = null)
    {
        var servicio = new FakeServicio();
        var formulario = new MigracionFormulario(servicio, new FakeSelector(seleccion ?? DirectoryPickerResult.Cancelled), NullLogger.Instance, accion =>
        {
            accion();
            return Task.CompletedTask;
        });
        await formulario.CargarAsync();
        return (formulario, servicio);
    }

    [Fact]
    public async Task SoloPuedeIniciarConProyectoYDirectorio()
    {
        var (formulario, _) = await CrearAsync();
        Assert.False(formulario.PuedeIniciar);

        formulario.ProyectoId = 1;
        Assert.False(formulario.PuedeIniciar);

        formulario.DirectorioSalida = @"C:\Salida\App10";
        Assert.True(formulario.PuedeIniciar);
    }

    [Fact]
    public async Task SeleccionarCarpeta_SoloRellenaElDirectorio()
    {
        var (formulario, servicio) = await CrearAsync(DirectoryPickerResult.Selected(@"C:\Salida"));
        formulario.ProyectoId = 1;

        await formulario.SeleccionarCarpetaAsync();

        Assert.Equal(@"C:\Salida", formulario.DirectorioSalida);
        Assert.Equal(0, servicio.Llamadas);
    }

    [Fact]
    public async Task CancelarSelector_ConservaElDirectorio()
    {
        var (formulario, _) = await CrearAsync(DirectoryPickerResult.Cancelled);
        formulario.DirectorioSalida = @"C:\Manual";

        await formulario.SeleccionarCarpetaAsync();

        Assert.Equal(@"C:\Manual", formulario.DirectorioSalida);
        Assert.Null(formulario.Mensaje);
    }

    [Fact]
    public async Task DuranteLaEjecucion_AplicaProgresoYNoPermiteOtraMigracion()
    {
        var (formulario, servicio) = await CrearAsync();
        formulario.ProyectoId = 1;
        formulario.DirectorioSalida = @"C:\Salida\App10";

        var ejecucion = formulario.IniciarAsync();
        Assert.True(formulario.Ejecutando);
        Assert.False(formulario.PuedeIniciar);

        await formulario.IniciarAsync();
        Assert.Equal(1, servicio.Llamadas);

        servicio.Progreso!.Report(MigrationProgress.StageChanged(MigrationStage.ValidacionProyecto, MigrationStageStatus.EnProceso));
        servicio.Progreso.Report(MigrationProgress.Log("Verificando proyecto..."));
        Assert.Equal(MigrationStageStatus.EnProceso, formulario.Etapas[MigrationStage.ValidacionProyecto]);
        Assert.Equal(MigrationStageStatus.Pendiente, formulario.Etapas[MigrationStage.Build]);
        Assert.Equal(new[] { "Verificando proyecto..." }, formulario.Bitacora);

        servicio.Resultado.SetResult(new MigrarProyectoResult(true, "Copia preparada.", Resumen("Exitosa")));
        await ejecucion;

        Assert.False(formulario.Ejecutando);
        Assert.Equal(OperationMessageKind.Success, formulario.MensajeTipo);
        Assert.Equal("Copia preparada.", formulario.Mensaje);
    }

    [Fact]
    public async Task ResultadoFallido_MuestraErrorYPermiteVerDetalle()
    {
        var (formulario, servicio) = await CrearAsync();
        formulario.ProyectoId = 1;
        formulario.DirectorioSalida = @"C:\Salida\App10";
        servicio.Resultado.SetResult(new MigrarProyectoResult(false, "El build falló.", Resumen("Fallida")));

        await formulario.IniciarAsync();
        Assert.Equal(OperationMessageKind.Error, formulario.MensajeTipo);

        formulario.AlternarDetalle();
        Assert.True(formulario.MostrarDetalle);
        formulario.AlternarDetalle();
        Assert.False(formulario.MostrarDetalle);
    }

    [Fact]
    public async Task SinResultadoDeMigracion_NoMuestraDetalle()
    {
        var (formulario, servicio) = await CrearAsync();
        formulario.ProyectoId = 1;
        formulario.DirectorioSalida = @"C:\Salida\App10";
        servicio.Resultado.SetResult(new MigrarProyectoResult(false, "No se encontró un SDK .NET 10."));

        await formulario.IniciarAsync();
        formulario.AlternarDetalle();

        Assert.False(formulario.MostrarDetalle);
    }
}
