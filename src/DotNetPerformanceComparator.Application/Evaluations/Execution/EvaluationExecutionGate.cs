namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Exclusión global mediante un semáforo de una sola posición (registrado como singleton).
/// Garantiza que no se ejecuten dos evaluaciones de rendimiento a la vez.
/// </summary>
public sealed class EvaluationExecutionGate : IEvaluationExecutionGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryAcquire() => _semaphore.Wait(0);

    public void Release() => _semaphore.Release();
}
