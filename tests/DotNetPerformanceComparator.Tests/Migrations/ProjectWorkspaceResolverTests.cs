using DotNetPerformanceComparator.Application.Projects.Workspace;
using DotNetPerformanceComparator.Infrastructure.Projects;

namespace DotNetPerformanceComparator.Tests.Migrations;

/// <summary>
/// Pruebas de <see cref="ProjectWorkspaceResolver"/> (BUG-01): resolución del grafo de ProjectReference y de la
/// raíz segura del workspace. Usa fixtures genéricos en disco (no depende de ninguna aplicación concreta).
/// </summary>
public sealed class ProjectWorkspaceResolverTests : IDisposable
{
    private readonly TempTree _tree = new();
    private readonly ProjectWorkspaceResolver _resolver = new();

    public void Dispose() => _tree.Dispose();

    private static string Lib(string refs = "") =>
        $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>{refs}</Project>";

    private static string Ref(params string[] rutas) =>
        "<ItemGroup>" + string.Join("", rutas.Select(r => $"<ProjectReference Include=\"{r}\" />")) + "</ItemGroup>";

    private ProjectWorkspaceInfo Resolver(string entry)
    {
        var r = _resolver.Resolve(entry);
        Assert.True(r.Success, r.Message);
        return r.Info!;
    }

    // ---------------------------------------------------------------- resolución del grafo

    [Fact]
    public void ProyectoSimpleSinReferencias_RaizEsElDirectorio()
    {
        var entry = _tree.File("App/App.csproj", Lib());

        var info = Resolver(entry);

        Assert.Equal(_tree.Path("App"), info.WorkspaceRoot);
        Assert.Equal("App.csproj", info.EntryProjectRelativePath);
        Assert.Empty(info.ReferencedProjectFiles);
    }

    [Fact]
    public void ReferenciaHermanaConDotDot_ResuelveAmbos()
    {
        var entry = _tree.File("Sol/src/App/App.csproj", Lib(Ref(@"..\Lib\Lib.csproj")));
        _tree.File("Sol/src/Lib/Lib.csproj", Lib());

        var info = Resolver(entry);

        Assert.Equal(2, info.ProjectFiles.Count);
        Assert.Single(info.ReferencedProjectFiles);
        Assert.Equal(_tree.Path("Sol", "src"), info.WorkspaceRoot); // sin solución → ancestro común
        Assert.Equal(Path.Combine("App", "App.csproj"), info.EntryProjectRelativePath);
    }

    [Fact]
    public void DosHermanos_ResuelveTres()
    {
        var entry = _tree.File("S/src/App/App.csproj", Lib(Ref(@"..\A\A.csproj", @"..\B\B.csproj")));
        _tree.File("S/src/A/A.csproj", Lib());
        _tree.File("S/src/B/B.csproj", Lib());

        Assert.Equal(3, Resolver(entry).ProjectFiles.Count);
    }

    [Fact]
    public void Transitiva_WebAppDomain()
    {
        var entry = _tree.File("S/src/Web/Web.csproj", Lib(Ref(@"..\App\App.csproj")));
        _tree.File("S/src/App/App.csproj", Lib(Ref(@"..\Domain\Domain.csproj")));
        _tree.File("S/src/Domain/Domain.csproj", Lib());

        var info = Resolver(entry);
        Assert.Equal(3, info.ProjectFiles.Count);
        Assert.Equal(2, info.ReferencedProjectFiles.Count);
    }

    [Fact]
    public void ReferenciaRepetida_SeDeduplica()
    {
        var entry = _tree.File("S/src/Web/Web.csproj", Lib(Ref(@"..\App\App.csproj", @"..\App\App.csproj")));
        _tree.File("S/src/App/App.csproj", Lib());

        Assert.Equal(2, Resolver(entry).ProjectFiles.Count); // App una sola vez
    }

    [Fact]
    public void CicloAccidental_NoProduceBucleInfinito()
    {
        var entry = _tree.File("S/A/A.csproj", Lib(Ref(@"..\B\B.csproj")));
        _tree.File("S/B/B.csproj", Lib(Ref(@"..\A\A.csproj")));

        Assert.Equal(2, Resolver(entry).ProjectFiles.Count); // A y B, sin colgarse
    }

    [Fact]
    public void RutaRelativaConBarraNormal_SeNormaliza()
    {
        var entry = _tree.File("S/src/App/App.csproj", Lib(Ref("../Lib/Lib.csproj")));
        _tree.File("S/src/Lib/Lib.csproj", Lib());

        Assert.Equal(2, Resolver(entry).ProjectFiles.Count);
    }

    // ---------------------------------------------------------------- errores y seguridad

    [Fact]
    public void ReferenciaInexistente_Error()
    {
        var entry = _tree.File("App/App.csproj", Lib(Ref(@"..\Nope\Nope.csproj")));

        var r = _resolver.Resolve(entry);
        Assert.False(r.Success);
        Assert.Equal(WorkspaceResolutionError.ReferenceNotFound, r.Error);
    }

    [Theory]
    [InlineData(@"$(RaizSolucion)\Lib\Lib.csproj")]
    [InlineData(@"..\@(Grupo)\Lib.csproj")]
    [InlineData(@"..\*\Lib.csproj")]
    public void ReferenciaConExpresionMsBuild_SeRechaza(string include)
    {
        var entry = _tree.File("App/App.csproj", Lib(Ref(include)));

        var r = _resolver.Resolve(entry);
        Assert.False(r.Success);
        Assert.Equal(WorkspaceResolutionError.UnresolvableReference, r.Error);
    }

    [Fact]
    public void EntryInexistente_Error()
    {
        var r = _resolver.Resolve(_tree.Path("No", "Existe.csproj"));
        Assert.False(r.Success);
        Assert.Equal(WorkspaceResolutionError.EntryProjectNotFound, r.Error);
    }

    // ---------------------------------------------------------------- raíz de solución / fallback

    [Fact]
    public void SolucionRaiz_SeDetecta()
    {
        _tree.File("Repo/App.sln", "sln");
        var entry = _tree.File("Repo/src/Web/Web.csproj", Lib(Ref(@"..\Lib\Lib.csproj")));
        _tree.File("Repo/src/Lib/Lib.csproj", Lib());

        var info = Resolver(entry);
        Assert.Equal(_tree.Path("Repo"), info.WorkspaceRoot); // raíz = directorio de la solución
        Assert.Equal(Path.Combine("src", "Web", "Web.csproj"), info.EntryProjectRelativePath);
    }

    [Fact]
    public void SinSolucion_UsaAncestroComun_DentroDelArbol()
    {
        var entry = _tree.File("Repo/src/Web/Web.csproj", Lib(Ref(@"..\Lib\Lib.csproj")));
        _tree.File("Repo/src/Lib/Lib.csproj", Lib());

        var info = Resolver(entry);
        Assert.Equal(_tree.Path("Repo", "src"), info.WorkspaceRoot);
        // Nunca devuelve una raíz de unidad: la raíz queda dentro del árbol temporal.
        Assert.StartsWith(_tree.Root, info.WorkspaceRoot, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProyectoSimpleEnSolucion_NoArrastraLaSolucion()
    {
        // Un proyecto único (sin ProjectReference) NO sube a la solución contenedora: su raíz es su directorio.
        _tree.File("Repo/Repo.sln", "sln");
        var entry = _tree.File("Repo/src/App/App.csproj", Lib());

        Assert.Equal(_tree.Path("Repo", "src", "App"), Resolver(entry).WorkspaceRoot);
    }
}
