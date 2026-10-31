using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

namespace DotNetPerformanceComparator.Infrastructure.ExperimentalMigration;

/// <summary>
/// Crea o actualiza el <c>global.json</c> de la raíz de la copia experimental para que apunte al SDK .NET 10
/// seleccionado por CU-02 (BUG-03), con la misma política (<see cref="SdkPinPolicy"/>) que usa el ejecutor de
/// comandos. Edita el JSON de forma segura con <see cref="JsonNode"/>: conserva <c>msbuild-sdks</c> y cualquier
/// otra propiedad del documento. Solo toca la copia; el original nunca se modifica.
/// </summary>
public sealed class GlobalJsonSynchronizer : IGlobalJsonSynchronizer
{
    private static readonly JsonDocumentOptions LecturaTolerante = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions Escritura = new() { WriteIndented = true };

    public GlobalJsonSyncResult Synchronize(string copyWorkspaceRoot, string sdkVersion)
    {
        var ruta = Path.Combine(copyWorkspaceRoot, "global.json");
        var existe = File.Exists(ruta);

        JsonObject raiz;
        if (existe)
        {
            JsonNode? nodo;
            try
            {
                nodo = JsonNode.Parse(File.ReadAllText(ruta), documentOptions: LecturaTolerante);
            }
            catch (JsonException)
            {
                return new GlobalJsonSyncResult(GlobalJsonSyncOutcome.Failed,
                    "No se pudo actualizar de forma segura el archivo global.json de la copia experimental (formato no válido).");
            }

            if (nodo is not JsonObject objeto)
            {
                return new GlobalJsonSyncResult(GlobalJsonSyncOutcome.Failed,
                    "No se pudo actualizar de forma segura el archivo global.json de la copia experimental (estructura no esperada).");
            }

            raiz = objeto;
        }
        else
        {
            raiz = new JsonObject();
        }

        // La sección "sdk" debe ser un objeto; si existe con otro tipo, el documento no es compatible.
        if (raiz.TryGetPropertyValue("sdk", out var sdkNodo) && sdkNodo is not JsonObject)
        {
            return new GlobalJsonSyncResult(GlobalJsonSyncOutcome.Failed,
                "No se pudo actualizar de forma segura el archivo global.json de la copia experimental (sección 'sdk' no válida).");
        }

        if (raiz["sdk"] is not JsonObject sdk)
        {
            sdk = new JsonObject();
            raiz["sdk"] = sdk;
        }

        // version/rollForward/allowPrerelease siguen la política controlada; se conservan otras propiedades de "sdk".
        sdk["version"] = sdkVersion;
        sdk["rollForward"] = SdkPinPolicy.RollForward;
        sdk["allowPrerelease"] = SdkPinPolicy.AllowPrerelease;

        try
        {
            File.WriteAllText(ruta, raiz.ToJsonString(Escritura));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new GlobalJsonSyncResult(GlobalJsonSyncOutcome.Failed,
                "No se pudo escribir el archivo global.json de la copia experimental.");
        }

        return new GlobalJsonSyncResult(existe ? GlobalJsonSyncOutcome.Updated : GlobalJsonSyncOutcome.Created);
    }
}
