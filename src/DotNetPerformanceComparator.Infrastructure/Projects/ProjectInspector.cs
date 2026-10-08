using System.Xml;
using System.Xml.Linq;
using DotNetPerformanceComparator.Application.Projects.Incorporation;

namespace DotNetPerformanceComparator.Infrastructure.Projects;

/// <summary>
/// Inspección de solo lectura de un <c>.csproj</c>: lee el XML directamente (sin evaluar MSBuild,
/// sin ejecutar ni compilar) y revisa únicamente la raíz inmediata del directorio del proyecto.
/// </summary>
public sealed class ProjectInspector : IProjectInspector
{
    private static readonly string[] CarpetasCandidatas =
        ["Areas", "Components", "Controllers", "Data", "Models", "Pages", "Services", "Views", "wwwroot"];

    private static readonly string[] ArchivosCandidatos =
        ["Program.cs", "appsettings.json", "appsettings.Development.json"];

    public Task<ProjectInspectionData> InspectAsync(string? csprojPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Inspect(csprojPath));
    }

    private static ProjectInspectionData Inspect(string? csprojPath)
    {
        if (string.IsNullOrWhiteSpace(csprojPath))
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.RutaVacia);
        }

        string fullPath;
        try
        {
            if (!Path.IsPathFullyQualified(csprojPath))
            {
                return ProjectInspectionData.Failed(ProjectFileStatus.RutaInvalida);
            }

            fullPath = Path.GetFullPath(csprojPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.RutaInvalida);
        }

        var projectName = Path.GetFileNameWithoutExtension(fullPath);
        if (!string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.ExtensionInvalida, fullPath);
        }

        var file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.NoExiste, fullPath, projectName);
        }

        if (file.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.EsEnlaceSimbolico, fullPath, projectName);
        }

        XDocument document;
        try
        {
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            document = XDocument.Load(reader);
        }
        catch (UnauthorizedAccessException)
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.AccesoDenegado, fullPath, projectName);
        }
        catch (XmlException)
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.ContenidoInvalido, fullPath, projectName);
        }
        catch (IOException)
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.NoLegible, fullPath, projectName);
        }

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Project")
        {
            return ProjectInspectionData.Failed(ProjectFileStatus.ContenidoInvalido, fullPath, projectName);
        }

        var directory = file.Directory!;
        return new ProjectInspectionData(
            ProjectFileStatus.Leido,
            fullPath,
            projectName,
            ReadSdks(root),
            ReadTargetFrameworks(root),
            FindKeyFolders(directory),
            FindDetectedFiles(directory, file.Name));
    }

    private static IReadOnlyList<string> ReadSdks(XElement root)
    {
        // <Project Sdk="A;B/1.0"> y <Sdk Name="A" />
        var fromAttribute = ((string?)root.Attribute("Sdk") ?? string.Empty).Split(';');
        var fromElements = root.Elements().Where(e => e.Name.LocalName == "Sdk").Select(e => (string?)e.Attribute("Name") ?? string.Empty);

        return fromAttribute.Concat(fromElements)
            .Select(s => s.Split('/')[0].Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ReadTargetFrameworks(XElement root) =>
        root.Descendants()
            .Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks")
            .SelectMany(e => e.Value.Split(';'))
            .Select(v => v.Trim().ToLowerInvariant())
            .Where(v => v.Length > 0)
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> FindKeyFolders(DirectoryInfo directory)
    {
        try
        {
            var existentes = directory.EnumerateDirectories().Select(d => d.Name).ToList();
            return CarpetasCandidatas
                .Select(c => existentes.FirstOrDefault(e => string.Equals(e, c, StringComparison.OrdinalIgnoreCase)))
                .OfType<string>()
                .ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> FindDetectedFiles(DirectoryInfo directory, string csprojName)
    {
        try
        {
            var existentes = directory.EnumerateFiles().Select(f => f.Name).ToList();
            return ArchivosCandidatos
                .Select(c => existentes.FirstOrDefault(e => string.Equals(e, c, StringComparison.OrdinalIgnoreCase)))
                .OfType<string>()
                .Prepend(csprojName)
                .ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return [csprojName];
        }
    }
}
