using DotNetPerformanceComparator.Application.Projects.Incorporation;
using DotNetPerformanceComparator.Web.Components.Pages;
using DotNetPerformanceComparator.Web.Components.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetPerformanceComparator.Tests.Web;

public class IncorporarProyectoFormularioTests
{
    private const string RutaAnterior = @"C:\Temp\Anterior\Anterior.csproj";
    private const string RutaNueva = @"C:\Temp\Nueva\Nueva.csproj";

    private sealed class FakeServicio : IIncorporarProyectoService
    {
        public Task<ProjectInspectionResult> InspeccionarAsync(string? rutaProyecto, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProjectInspectionResult(
                rutaProyecto, "Anterior", "ASP.NET Core Web", ".NET 8", "net8.0", [], [],
                ProjectInspectionState.Valido, [], "El proyecto cumple las condiciones."));

        public Task<IncorporarProyectoResult> RegistrarAsync(IncorporarProyectoRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IncorporarProyectoResult(true, "Registrado.", 1));
    }

    private sealed class FakeSelector : IProjectFilePicker
    {
        public TaskCompletionSource<ProjectFilePickerResult> Resultado { get; } = new();
        public int Llamadas { get; private set; }

        public Task<ProjectFilePickerResult> PickProjectFileAsync(CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Resultado.Task;
        }
    }

    private static (IncorporarProyectoFormulario Formulario, FakeSelector Selector) Crear()
    {
        var selector = new FakeSelector();
        return (new IncorporarProyectoFormulario(new FakeServicio(), selector, NullLogger.Instance), selector);
    }

    [Fact]
    public async Task Seleccion_ColocaLaRutaSinValidar()
    {
        var (formulario, selector) = Crear();
        selector.Resultado.SetResult(ProjectFilePickerResult.Selected(RutaNueva));

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(RutaNueva, formulario.Ruta);
        Assert.Null(formulario.Inspeccion);
        Assert.False(formulario.PuedeGuardar);
        Assert.Null(formulario.Mensaje);
    }

    [Fact]
    public async Task Cancelacion_ConservaRutaEInspeccionSinMensaje()
    {
        var (formulario, selector) = Crear();
        formulario.Ruta = RutaAnterior;
        await formulario.ValidarAsync();
        var inspeccion = formulario.Inspeccion;
        var mensaje = formulario.Mensaje;
        selector.Resultado.SetResult(ProjectFilePickerResult.Cancelled);

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(RutaAnterior, formulario.Ruta);
        Assert.Same(inspeccion, formulario.Inspeccion);
        Assert.Equal(mensaje, formulario.Mensaje);
        Assert.True(formulario.PuedeGuardar);
    }

    [Theory]
    [InlineData(RutaNueva)]
    [InlineData(RutaAnterior)]
    public async Task Seleccion_DescartaLaInspeccionAnterior(string seleccionada)
    {
        var (formulario, selector) = Crear();
        formulario.Ruta = RutaAnterior;
        await formulario.ValidarAsync();
        Assert.True(formulario.PuedeGuardar);
        selector.Resultado.SetResult(ProjectFilePickerResult.Selected(seleccionada));

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(seleccionada, formulario.Ruta);
        Assert.Null(formulario.Inspeccion);
        Assert.False(formulario.PuedeGuardar);
    }

    [Fact]
    public async Task EdicionManualDeLaRuta_DescartaLaInspeccionAnterior()
    {
        var (formulario, _) = Crear();
        formulario.Ruta = RutaAnterior;
        await formulario.ValidarAsync();

        formulario.Ruta = RutaNueva;

        Assert.Null(formulario.Inspeccion);
        Assert.False(formulario.PuedeGuardar);
    }

    [Fact]
    public async Task Error_MuestraMensajeYConservaLaRuta()
    {
        var (formulario, selector) = Crear();
        formulario.Ruta = RutaAnterior;
        selector.Resultado.SetResult(ProjectFilePickerResult.Error);

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(RutaAnterior, formulario.Ruta);
        Assert.Equal(OperationMessageKind.Error, formulario.MensajeTipo);
        Assert.Contains("selector de archivos", formulario.Mensaje);
    }

    [Fact]
    public async Task ExcepcionDelSelector_SeConvierteEnMensajeSinDetalleTecnico()
    {
        var (formulario, selector) = Crear();
        selector.Resultado.SetException(new InvalidOperationException("detalle COM interno"));

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(OperationMessageKind.Error, formulario.MensajeTipo);
        Assert.DoesNotContain("detalle COM interno", formulario.Mensaje);
        Assert.False(formulario.SeleccionandoArchivo);
    }

    [Fact]
    public async Task PlataformaNoCompatible_InformaYMantieneIngresoManual()
    {
        var (formulario, selector) = Crear();
        formulario.Ruta = RutaAnterior;
        selector.Resultado.SetResult(ProjectFilePickerResult.Unsupported);

        await formulario.SeleccionarArchivoAsync();

        Assert.Equal(RutaAnterior, formulario.Ruta);
        Assert.Equal(OperationMessageKind.Warning, formulario.MensajeTipo);
        Assert.Contains("Windows", formulario.Mensaje);
    }

    [Fact]
    public async Task NoPermiteAbrirDosSelectoresSimultaneos()
    {
        var (formulario, selector) = Crear();

        var primera = formulario.SeleccionarArchivoAsync();
        Assert.True(formulario.SeleccionandoArchivo);

        await formulario.SeleccionarArchivoAsync();
        Assert.Equal(1, selector.Llamadas);

        selector.Resultado.SetResult(ProjectFilePickerResult.Selected(RutaNueva));
        await primera;

        Assert.False(formulario.SeleccionandoArchivo);
        Assert.Equal(RutaNueva, formulario.Ruta);
    }
}
