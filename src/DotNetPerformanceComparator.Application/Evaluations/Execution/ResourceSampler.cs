namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Muestrea CPU/RAM del proceso objetivo cada <c>intervaloMuestreoMs</c> hasta que se cancela el token,
/// acumulando las muestras en <paramref name="muestras"/>. Reutilizado por Carga (ventana estable) y
/// Estrés (una ventana por nivel), con la misma semántica auditada en C-07.1. El token delimita la ventana.
/// </summary>
public static class ResourceSampler
{
    public static async Task RunAsync(
        ITargetApp app,
        List<ResourceSample> muestras,
        int intervaloMuestreoMs,
        int processorCount,
        Action<ResourceAggregates>? onParcial,
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var muestra = app.Sample();
                if (muestra is not null)
                {
                    muestras.Add(muestra);
                    if (onParcial is not null && muestras.Count >= 2)
                    {
                        onParcial(ResourceAggregator.Aggregate(muestras.TakeLast(2).ToList(), processorCount));
                    }
                }

                await Task.Delay(intervaloMuestreoMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Fin normal de la ventana de muestreo.
        }
    }
}
