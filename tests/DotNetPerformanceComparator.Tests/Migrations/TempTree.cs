using System.Security.Cryptography;

namespace DotNetPerformanceComparator.Tests.Migrations;

/// <summary>Árbol de directorios temporal creado por una prueba y eliminado al terminar.</summary>
public sealed class TempTree : IDisposable
{
    public const string WebNet8 = """
        <Project Sdk="Microsoft.NET.Sdk.Web">

          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>

          <ItemGroup>
            <PackageReference Include="Serilog" Version="3.1.1" />
          </ItemGroup>

        </Project>
        """;

    public string Root { get; } = Directory.CreateTempSubdirectory("dnpc-migracion-").FullName;

    public string Path(params string[] partes) => System.IO.Path.Combine([Root, .. partes]);

    public string File(string relativa, string contenido = "contenido")
    {
        var ruta = Path(relativa);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta)!);
        System.IO.File.WriteAllText(ruta, contenido);
        return ruta;
    }

    public string Dir(string relativa) => Directory.CreateDirectory(Path(relativa)).FullName;

    /// <summary>Huella SHA-256 de todos los archivos (ruta relativa + contenido) bajo un directorio.</summary>
    public static string Huella(string directorio)
    {
        using var sha = SHA256.Create();
        foreach (var archivo in Directory.EnumerateFiles(directorio, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var nombre = System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetRelativePath(directorio, archivo));
            sha.TransformBlock(nombre, 0, nombre.Length, null, 0);
            var bytes = System.IO.File.ReadAllBytes(archivo);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
