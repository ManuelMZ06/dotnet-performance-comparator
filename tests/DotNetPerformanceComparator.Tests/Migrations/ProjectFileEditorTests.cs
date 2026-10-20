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

    // La detección de ProjectReference externos se trasladó a ProjectWorkspaceResolver (BUG-01),
    // con su propia batería de pruebas (ProjectWorkspaceResolverTests).

    // ---------------------------------------------------------------- análisis de TFM (BUG-02)

    private string Lib(string xml) => _tree.File($"Lib{Guid.NewGuid():N}/Lib.csproj", xml);

    [Fact]
    public void Analyze_Net8_RequiereCambio()
    {
        var a = _editor.AnalyzeTargetFramework(Lib("""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>"""));
        Assert.True(a.Resoluble);
        Assert.True(a.RequiresChange);
        Assert.Equal(new[] { "net10.0" }, a.FrameworksDestino);
    }

    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("netstandard2.1")]
    [InlineData("net7.0")]
    [InlineData("net10.0")]
    public void Analyze_NoNet8_SeConserva(string tfm)
    {
        var a = _editor.AnalyzeTargetFramework(Lib($"""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>{tfm}</TargetFramework></PropertyGroup></Project>"""));
        Assert.True(a.Resoluble);
        Assert.False(a.RequiresChange); // se conserva: la herramienta migra net8→net10, no cualquier TFM
    }

    [Fact]
    public void Analyze_Expresion_NoResoluble()
    {
        var a = _editor.AnalyzeTargetFramework(Lib("""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>$(DefaultTfm)</TargetFramework></PropertyGroup></Project>"""));
        Assert.False(a.Resoluble);
        Assert.Contains("expresión MSBuild", a.Motivo);
    }

    [Fact]
    public void Analyze_SinTargetFramework_NoResoluble()
    {
        // Sin TargetFramework explícito (heredado/no SDK-style): no se puede determinar con seguridad.
        var a = _editor.AnalyzeTargetFramework(Lib("""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>"""));
        Assert.False(a.Resoluble);
    }

    [Fact]
    public void TargetFrameworkNet8_SeCambiaANet10SinTocarOtrosElementos()
    {
        var original = Proyecto("<TargetFramework>net8.0</TargetFramework>");
        var csproj = _tree.File("App/App.csproj", original);

        var resultado = _editor.UpdateTargetFramework(csproj);

        Assert.True(resultado.Updated);
        Assert.Equal(new[] { "net10.0" }, resultado.TargetFrameworks);

        // Se compara el contenido lógico ignorando el estilo de salto de línea: el editor normaliza los
        // finales a LF al reescribir el XML, mientras que el esperado hereda el estilo del archivo .cs
        // según el checkout (LF local, CRLF en un checkout limpio de Windows). Solo interesa que net8.0
        // pase a net10.0 sin tocar lo demás.
        static string SinEstiloDeSalto(string texto) => texto.Replace("\r\n", "\n");
        var esperado = original.Replace("<TargetFramework>net8.0</TargetFramework>", "<TargetFramework>net10.0</TargetFramework>");
        Assert.Equal(SinEstiloDeSalto(esperado), SinEstiloDeSalto(File.ReadAllText(csproj)));
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
