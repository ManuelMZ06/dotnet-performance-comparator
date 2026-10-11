using DotNetPerformanceComparator.Domain.Constants;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>Un paso del plan: qué versión ejecutar y a qué repetición (<c>NumeroEjecucion</c>) pertenece.</summary>
public sealed record PasoEjecucion(string VersionDotNet, int NumeroEjecucion);

/// <summary>
/// Genera la secuencia experimental alternando el orden de .NET 8 y .NET 10 por repetición, para que
/// ninguna versión tenga sistemáticamente la ventaja/desventaja de ejecutarse primero.
/// </summary>
public static class EvaluationExecutionPlan
{
    /// <summary>
    /// Repetición impar → (.NET 8, .NET 10); repetición par → (.NET 10, .NET 8).
    /// Cada par comparte <c>NumeroEjecucion</c> = número de repetición.
    /// </summary>
    public static IReadOnlyList<PasoEjecucion> BuildSequence(int numeroEjecuciones)
    {
        var pasos = new List<PasoEjecucion>(numeroEjecuciones * 2);
        for (var repeticion = 1; repeticion <= numeroEjecuciones; repeticion++)
        {
            var (primera, segunda) = repeticion % 2 == 1
                ? (VersionesDotNet.Net8, VersionesDotNet.Net10)
                : (VersionesDotNet.Net10, VersionesDotNet.Net8);

            pasos.Add(new PasoEjecucion(primera, repeticion));
            pasos.Add(new PasoEjecucion(segunda, repeticion));
        }

        return pasos;
    }
}
