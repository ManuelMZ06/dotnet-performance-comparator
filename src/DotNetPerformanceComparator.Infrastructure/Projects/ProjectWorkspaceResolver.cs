using System.Xml;
using System.Xml.Linq;
using DotNetPerformanceComparator.Application.Projects.Workspace;
using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

namespace DotNetPerformanceComparator.Infrastructure.Projects;

/// <summary>
/// Resuelve el grafo de <c>ProjectReference</c> locales (recursivo, con control de ciclos/duplicados) y la raíz
/// segura del workspace a copiar. Lee el XML del .csproj de forma segura (sin DTD ni recursos externos, sin
/// evaluar MSBuild). No modifica nada. Soporta soluciones multiproyecto preservando su estructura (BUG-01).
/// </summary>
public sealed class ProjectWorkspaceResolver : IProjectWorkspaceResolver
{
    private static readonly XmlReaderSettings LecturaSegura = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null
    };

    public WorkspaceResolution Resolve(string entryCsprojPath)
    {
        string entry;
        try
        {
            entry = Path.GetFullPath(entryCsprojPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return WorkspaceResolution.Fail(WorkspaceResolutionError.EntryProjectNotFound, "La ruta del proyecto de entrada no es válida.");
        }

        if (!File.Exists(entry))
        {
            return WorkspaceResolution.Fail(WorkspaceResolutionError.EntryProjectNotFound, "No se encontró el archivo de proyecto de entrada.");
        }

        // Recorrido en anchura del grafo de ProjectReference (visitados por ruta canónica → sin duplicados ni ciclos).
        var proyectos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cola = new Queue<string>();
        cola.Enqueue(entry);

        while (cola.Count > 0)
        {
            var proyecto = cola.Dequeue();
            if (!proyectos.TryAdd(proyecto, proyecto))
            {
                continue;
            }

            var directorio = Path.GetDirectoryName(proyecto)!;
            IReadOnlyList<string> referencias;
            try
            {
                referencias = LeerProjectReferences(proyecto);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
            {
                return WorkspaceResolution.Fail(WorkspaceResolutionError.ReferenceNotFound,
                    $"No fue posible leer el archivo de proyecto '{Path.GetFileName(proyecto)}'.");
            }

            foreach (var include in referencias)
            {
                if (include.Contains("$(") || include.Contains("@(") || include.Contains('*'))
                {
                    return WorkspaceResolution.Fail(WorkspaceResolutionError.UnresolvableReference,
                        $"La referencia de proyecto '{include}' usa una expresión MSBuild o comodín que no puede resolverse de forma segura con esta versión de la herramienta.");
                }

                string destino;
                try
                {
                    destino = Path.GetFullPath(Path.Combine(directorio, include.Replace('/', Path.DirectorySeparatorChar)));
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    return WorkspaceResolution.Fail(WorkspaceResolutionError.ReferenceNotFound,
                        $"La referencia de proyecto '{include}' no apunta a una ruta válida.");
                }

                if (!File.Exists(destino))
                {
                    return WorkspaceResolution.Fail(WorkspaceResolutionError.ReferenceNotFound,
                        $"No se encontró el proyecto referenciado '{include}'.");
                }

                if (!proyectos.ContainsKey(destino))
                {
                    cola.Enqueue(destino);
                }
            }
        }

        var proyectosArchivos = proyectos.Values.ToList();
        var raiz = DeterminarRaiz(proyectosArchivos);
        if (raiz is null)
        {
            return WorkspaceResolution.Fail(WorkspaceResolutionError.NoSafeRoot,
                "No fue posible determinar una raíz de workspace segura que contenga el proyecto y todas sus referencias dentro de un mismo directorio.");
        }

        var relativa = Path.GetRelativePath(raiz, entry);
        var referenciados = proyectosArchivos
            .Where(p => !string.Equals(p, entry, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var info = new ProjectWorkspaceInfo(raiz, entry, relativa, proyectosArchivos, referenciados);
        return WorkspaceResolution.Ok(info);
    }

    private static IReadOnlyList<string> LeerProjectReferences(string csprojPath)
    {
        using var reader = XmlReader.Create(new StringReader(File.ReadAllText(csprojPath)), LecturaSegura);
        var documento = XDocument.Load(reader);

        return documento.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
            .SelectMany(i => i.Split(';'))
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .ToList();
    }

    /// <summary>Máximo de niveles por encima del ancestro común donde se busca el directorio de solución.</summary>
    private const int MaxNivelesSolucion = 3;

    /// <summary>
    /// Raíz segura a copiar:
    /// <list type="bullet">
    /// <item>Un único proyecto (sin <c>ProjectReference</c>): su propio directorio (no se sube a ninguna solución).</item>
    /// <item>Multiproyecto: el directorio de solución (*.sln/*.slnx) más cercano desde el ancestro común hacia
    /// arriba (acotado a <see cref="MaxNivelesSolucion"/> niveles); si no hay solución, el propio ancestro común.</item>
    /// </list>
    /// Devuelve null si el ancestro común es una raíz de unidad (p. ej. <c>C:\</c>) o no existe (unidades distintas).
    /// </summary>
    private static string? DeterminarRaiz(IReadOnlyList<string> proyectosArchivos)
    {
        var directorios = proyectosArchivos
            .Select(p => PathRules.NormalizeDirectory(Path.GetDirectoryName(p)!))
            .ToList();

        var comun = AncestroComun(directorios);
        if (comun is null || EsRaizDeUnidad(comun))
        {
            return null;
        }

        // Un solo proyecto: la raíz es su directorio. No se sube a una solución contenedora (podría arrastrar
        // un repositorio entero). Preserva el comportamiento de proyectos simples.
        if (proyectosArchivos.Count == 1)
        {
            return comun;
        }

        var dir = comun;
        var niveles = 0;
        while (dir is not null && !EsRaizDeUnidad(dir) && niveles <= MaxNivelesSolucion)
        {
            if (TieneSolucion(dir))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
            niveles++;
        }

        return comun;
    }

    /// <summary>
    /// true si el directorio contiene un archivo de solución. Compara la extensión exactamente: el comodín
    /// <c>*.sln</c> de <c>EnumerateFiles</c> también coincidiría con <c>.slnx</c> u otros por los nombres cortos 8.3.
    /// </summary>
    private static bool TieneSolucion(string directorio)
    {
        try
        {
            return Directory.EnumerateFiles(directorio).Any(f =>
            {
                var ext = Path.GetExtension(f);
                return ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
            });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Un directorio raíz de unidad/volumen, p. ej. <c>C:\</c> o <c>\\srv\share</c> (no copiable como workspace).</summary>
    private static bool EsRaizDeUnidad(string directorio)
    {
        // "C:" sin separador resolvería al directorio actual de la unidad; se añade el separador para evitarlo.
        var conSeparador = directorio.EndsWith(':') ? directorio + Path.DirectorySeparatorChar : directorio;
        string full;
        try
        {
            full = Path.GetFullPath(conSeparador);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return true; // ante la duda, tratarlo como límite no copiable
        }

        var raiz = Path.GetPathRoot(full);
        return !string.IsNullOrEmpty(raiz)
            && string.Equals(Path.TrimEndingDirectorySeparator(full), Path.TrimEndingDirectorySeparator(raiz), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Directorio ancestro común de todos los directorios (segmentos exactos, sin distinguir mayúsculas).</summary>
    private static string? AncestroComun(IReadOnlyList<string> directorios)
    {
        if (directorios.Count == 0)
        {
            return null;
        }

        var referencia = directorios[0].Split(Path.DirectorySeparatorChar);
        var comunes = referencia.Length;

        foreach (var directorio in directorios.Skip(1))
        {
            var segmentos = directorio.Split(Path.DirectorySeparatorChar);
            var limite = Math.Min(comunes, segmentos.Length);
            var i = 0;
            while (i < limite && string.Equals(referencia[i], segmentos[i], StringComparison.OrdinalIgnoreCase))
            {
                i++;
            }

            comunes = i;
            if (comunes == 0)
            {
                return null; // sin raíz común (p. ej. unidades distintas)
            }
        }

        var prefijo = string.Join(Path.DirectorySeparatorChar, referencia.Take(comunes));
        // En Windows un prefijo de una sola parte ("C:") necesita el separador para ser una raíz de unidad válida.
        if (prefijo.EndsWith(':'))
        {
            prefijo += Path.DirectorySeparatorChar;
        }

        return prefijo;
    }
}
