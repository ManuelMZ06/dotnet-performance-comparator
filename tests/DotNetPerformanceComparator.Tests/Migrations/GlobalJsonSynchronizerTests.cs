using System.Text.Json.Nodes;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;
using DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

namespace DotNetPerformanceComparator.Tests.Migrations;

/// <summary>
/// Pruebas de <see cref="GlobalJsonSynchronizer"/> (BUG-03): crear/actualizar el global.json de la copia con
/// el SDK .NET 10 seleccionado, preservando otras propiedades y fallando de forma controlada ante JSON inválido.
/// </summary>
public sealed class GlobalJsonSynchronizerTests : IDisposable
{
    private readonly TempTree _tree = new();
    private readonly GlobalJsonSynchronizer _sync = new();

    public void Dispose() => _tree.Dispose();

    private JsonObject LeerGlobal(string root) =>
        (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(root, "global.json")))!;

    [Fact]
    public void Crea_SiNoExiste()
    {
        var root = _tree.Dir("Copia");

        var r = _sync.Synchronize(root, "10.0.400");

        Assert.Equal(GlobalJsonSyncOutcome.Created, r.Outcome);
        var sdk = (JsonObject)LeerGlobal(root)["sdk"]!;
        Assert.Equal("10.0.400", (string?)sdk["version"]);
        Assert.Equal("disable", (string?)sdk["rollForward"]);       // misma política que el ejecutor interno
        Assert.False((bool)sdk["allowPrerelease"]!);
    }

    [Fact]
    public void Actualiza_PreservandoOtrasPropiedades()
    {
        var root = _tree.Dir("Copia");
        _tree.File("Copia/global.json", """
            {
              "sdk": { "version": "8.0.425", "rollForward": "latestFeature" },
              "msbuild-sdks": { "Example.Custom.Sdk": "1.2.3" },
              "custom-property": { "enabled": true }
            }
            """);

        var r = _sync.Synchronize(root, "10.0.400");

        Assert.Equal(GlobalJsonSyncOutcome.Updated, r.Outcome);
        var global = LeerGlobal(root);
        var sdk = (JsonObject)global["sdk"]!;
        Assert.Equal("10.0.400", (string?)sdk["version"]);          // versión actualizada
        Assert.Equal("disable", (string?)sdk["rollForward"]);        // política controlada
        Assert.NotNull(global["msbuild-sdks"]);                      // preservado
        Assert.Equal("1.2.3", (string?)global["msbuild-sdks"]!["Example.Custom.Sdk"]);
        Assert.True((bool)global["custom-property"]!["enabled"]!);   // propiedad raíz desconocida preservada
    }

    [Fact]
    public void JsonInvalido_FallaSinSobrescribir()
    {
        var root = _tree.Dir("Copia");
        var ruta = _tree.File("Copia/global.json", "{ esto no es json válido ");

        var r = _sync.Synchronize(root, "10.0.400");

        Assert.Equal(GlobalJsonSyncOutcome.Failed, r.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(r.Error));
        Assert.Equal("{ esto no es json válido ", File.ReadAllText(ruta)); // no se sobrescribió
    }

    [Fact]
    public void SdkNoEsObjeto_Falla()
    {
        var root = _tree.Dir("Copia");
        _tree.File("Copia/global.json", """{ "sdk": "8.0.425" }""");

        Assert.Equal(GlobalJsonSyncOutcome.Failed, _sync.Synchronize(root, "10.0.400").Outcome);
    }

    [Fact]
    public void PreservaOtrasPropiedadesDentroDeSdk()
    {
        var root = _tree.Dir("Copia");
        _tree.File("Copia/global.json", """{ "sdk": { "version": "8.0.425", "otraClave": "valor" } }""");

        _sync.Synchronize(root, "10.0.400");

        var sdk = (JsonObject)LeerGlobal(root)["sdk"]!;
        Assert.Equal("10.0.400", (string?)sdk["version"]);
        Assert.Equal("valor", (string?)sdk["otraClave"]); // propiedad no administrada dentro de sdk se conserva
    }
}
