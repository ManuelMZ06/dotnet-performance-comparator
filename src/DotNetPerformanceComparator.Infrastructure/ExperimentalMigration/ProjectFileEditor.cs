using System.Text;
using System.Xml;
using System.Xml.Linq;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

/// <summary>
/// Lectura y edición mínima del XML del <c>.csproj</c> (sin evaluar MSBuild ni procesar DTD).
/// La edición conserva espacios, codificación y declaración XML del archivo.
/// </summary>
public sealed class ProjectFileEditor : IProjectFileEditor
{
    private static readonly XmlReaderSettings LecturaSegura = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };

    public IReadOnlyList<string> FindExternalProjectReferences(string csprojPath, string rootDirectory)
    {
        var documento = Cargar(File.ReadAllText(csprojPath), preservarEspacios: false);
        var directorioProyecto = Path.GetDirectoryName(Path.GetFullPath(csprojPath))!;

        var externas = new List<string>();
        foreach (var include in documento.Descendants()
                     .Where(e => e.Name.LocalName == "ProjectReference")
                     .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
                     .SelectMany(i => i.Split(';'))
                     .Select(i => i.Trim())
                     .Where(i => i.Length > 0))
        {
            // Rutas con propiedades/elementos MSBuild o comodines no pueden resolverse sin evaluar MSBuild.
            if (include.Contains("$(") || include.Contains("@(") || include.Contains('*'))
            {
                externas.Add(include);
                continue;
            }

            var destino = Path.GetFullPath(Path.Combine(directorioProyecto, include.Replace('/', Path.DirectorySeparatorChar)));
            if (!PathRules.IsSameOrInside(destino, rootDirectory))
            {
                externas.Add(include);
            }
        }

        return externas;
    }

    public TargetFrameworkUpdateResult UpdateTargetFramework(string csprojPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(csprojPath);
            var conBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var texto = new UTF8Encoding(false).GetString(bytes, conBom ? 3 : 0, bytes.Length - (conBom ? 3 : 0));

            var documento = Cargar(texto, preservarEspacios: true);
            var actualizado = false;
            foreach (var elemento in ElementosTargetFramework(documento))
            {
                var (valor, cambio) = ReemplazarNet8(elemento.Value);
                if (cambio)
                {
                    elemento.Value = valor;
                    actualizado = true;
                }
            }

            if (!actualizado)
            {
                return new TargetFrameworkUpdateResult(false, Frameworks(documento), $"El archivo de proyecto no contiene {VersionesDotNet.Net8Tfm}.");
            }

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(conBom),
                OmitXmlDeclaration = documento.Declaration is null,
                Indent = false,
                NewLineHandling = NewLineHandling.None
            };

            using (var stream = new FileStream(csprojPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = XmlWriter.Create(stream, settings))
            {
                documento.Save(writer);
            }

            return new TargetFrameworkUpdateResult(true, Frameworks(documento));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            return new TargetFrameworkUpdateResult(false, [], "No fue posible leer o escribir el archivo de proyecto de la copia.");
        }
    }

    /// <summary>
    /// Reemplaza la entrada <c>net8.0</c> por <c>net10.0</c> conservando el resto de entradas y su formato.
    /// Si <c>net10.0</c> ya está presente, solo se retira <c>net8.0</c> para no duplicarlo.
    /// </summary>
    private static (string Valor, bool Cambio) ReemplazarNet8(string valor)
    {
        var segmentos = valor.Split(';');
        var tieneNet10 = segmentos.Any(s => s.Trim().Equals(VersionesDotNet.Net10Tfm, StringComparison.OrdinalIgnoreCase));
        var resultado = new List<string>(segmentos.Length);
        var cambio = false;

        foreach (var segmento in segmentos)
        {
            var tfm = segmento.Trim();
            if (!tfm.Equals(VersionesDotNet.Net8Tfm, StringComparison.OrdinalIgnoreCase))
            {
                resultado.Add(segmento);
                continue;
            }

            cambio = true;
            if (!tieneNet10)
            {
                resultado.Add(segmento.Replace(tfm, VersionesDotNet.Net10Tfm));
                tieneNet10 = true;
            }
        }

        return (string.Join(';', resultado), cambio);
    }

    private static IEnumerable<XElement> ElementosTargetFramework(XDocument documento) =>
        documento.Descendants().Where(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks").ToList();

    private static IReadOnlyList<string> Frameworks(XDocument documento) =>
        ElementosTargetFramework(documento)
            .SelectMany(e => e.Value.Split(';'))
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static XDocument Cargar(string texto, bool preservarEspacios)
    {
        using var reader = XmlReader.Create(new StringReader(texto), LecturaSegura);
        return XDocument.Load(reader, preservarEspacios ? LoadOptions.PreserveWhitespace : LoadOptions.None);
    }
}
