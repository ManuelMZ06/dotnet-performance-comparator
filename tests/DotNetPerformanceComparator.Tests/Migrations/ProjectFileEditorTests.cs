using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

namespace DotNetPerformanceComparator.Tests.Migrations;

public sealed class ProjectFileEditorTests : IDisposable
{
    private readonly TempTree _tree = new();
    private readonly ProjectFileEditor _editor = new();

    public void Dispose() => _tree.Dispose();

    private static string Proyecto(string propiedades, string items = "") => $"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            {propiedades}
            <AssemblyName>MiApp</AssemblyName>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Serilog" Version="3.1.1" />
            {items}
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void ReferenciaInterna_EsPermitida()
    {
        var csproj = _tree.File("App/App.csproj", Proyecto("<TargetFramework>net8.0</TargetFramework>",
            """<ProjectReference Include="Modulos\Datos\Datos.csproj" />"""));

        Assert.Empty(_editor.FindExternalProjectReferences(csproj, _tree.Path("App")));
    }

    [Theory]
    [InlineData(@"..\Compartido\Compartido.csproj")]
    [InlineData(@"C:\Otro\Lib.csproj")]
    [InlineData(@"$(RaizSolucion)\Lib\Lib.csproj")]
    public void ReferenciaExterna_EsDetectada(string include)
    {
        var csproj = _tree.File("App/App.csproj", Proyecto("<TargetFramework>net8.0</TargetFramework>",
            $"""<ProjectReference Include="{include}" />"""));

        Assert.Equal(new[] { include }, _editor.FindExternalProjectReferences(csproj, _tree.Path("App")));
    }

    [Fact]
    public void TargetFrameworkNet8_SeCambiaANet10SinTocarOtrosElementos()
    {
        var original = Proyecto("<TargetFramework>net8.0</TargetFramework>");
        var csproj = _tree.File("App/App.csproj", original);

        var resultado = _editor.UpdateTargetFramework(csproj);

        Assert.True(resultado.Updated);
        Assert.Equal(new[] { "net10.0" }, resultado.TargetFrameworks);
        Assert.Equal(original.Replace("<TargetFramework>net8.0</TargetFramework>", "<TargetFramework>net10.0</TargetFramework>"), File.ReadAllText(csproj));
    }

    [Fact]
    public void Multitarget_ReemplazaSoloNet8()
    {
        var csproj = _tree.File("App/App.csproj", Proyecto("<TargetFrameworks>net8.0; net9.0</TargetFrameworks>"));

        var resultado = _editor.UpdateTargetFramework(csproj);

        Assert.True(resultado.Updated);
        Assert.Equal(new[] { "net10.0", "net9.0" }, resultado.TargetFrameworks);
        Assert.Contains("<TargetFrameworks>net10.0; net9.0</TargetFrameworks>", File.ReadAllText(csproj));
    }

    [Fact]
    public void MultitargetConNet10_NoLoDuplica()
    {
        var csproj = _tree.File("App/App.csproj", Proyecto("<TargetFrameworks>net8.0;net10.0</TargetFrameworks>"));

        var resultado = _editor.UpdateTargetFramework(csproj);

        Assert.True(resultado.Updated);
        Assert.Equal(new[] { "net10.0" }, resultado.TargetFrameworks);
        Assert.Contains("<TargetFrameworks>net10.0</TargetFrameworks>", File.ReadAllText(csproj));
    }

    [Fact]
    public void SinNet8_NoModificaElArchivo()
    {
        var original = Proyecto("<TargetFramework>net9.0</TargetFramework>");
        var csproj = _tree.File("App/App.csproj", original);

        var resultado = _editor.UpdateTargetFramework(csproj);

        Assert.False(resultado.Updated);
        Assert.Equal(original, File.ReadAllText(csproj));
    }

    [Fact]
    public void ConservaBomYDeclaracionXml()
    {
        var conDeclaracion = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" + Proyecto("<TargetFramework>net8.0</TargetFramework>");
        var csproj = _tree.Path("App.csproj");
        File.WriteAllText(csproj, conDeclaracion, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        _editor.UpdateTargetFramework(csproj);

        var bytes = File.ReadAllBytes(csproj);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var texto = File.ReadAllText(csproj);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", texto);
        Assert.Contains("<PackageReference Include=\"Serilog\" Version=\"3.1.1\" />", texto);
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", texto);
    }
}
