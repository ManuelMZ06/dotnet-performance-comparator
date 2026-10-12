using System.Collections.Concurrent;
using System.Diagnostics;

namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Coordinador asíncrono de rondas sincronizadas de Concurrencia (C-07.4). Por ronda ofrece una
/// <b>barrera de inicio</b> (espera a N participantes distintos y los libera a todos de golpe) y una
/// <b>barrera de fin</b> (la ronda no termina hasta que las N respuestas finalizan). La ronda siguiente no
/// se habilita hasta que la anterior terminó y transcurrió el intervalo entre rondas. Puro y testeable,
/// sin NBomber: usa <see cref="TaskCompletionSource"/>/<see cref="Interlocked"/> y <see cref="CancellationToken"/>
/// (nunca bloqueos síncronos). Registra el desfase de inicio real como diagnóstico.
/// </summary>
public sealed class RoundCoordinator
{
    private sealed class Ronda
    {
        public readonly TaskCompletionSource Permitida = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource InicioLiberado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource FinCompletado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentDictionary<int, byte> InicioLlegados = new();
        public readonly ConcurrentDictionary<int, byte> FinLlegados = new();
        public readonly ConcurrentDictionary<int, long> Timestamps = new();
    }

    private readonly int _usuarios;
    private readonly int _rondas;
    private readonly int _intervaloRondasMs;
    private readonly int _timeoutMs;
    private readonly Action<int>? _onRondaLiberada;
    private readonly Action<int, double>? _onRondaCompletada;
    private readonly Ronda[] _r;
    private readonly List<double> _skewPorRonda = [];
    private readonly ConcurrentDictionary<int, int> _rondaPorInstancia = new();
    private volatile bool _faulted;

    public RoundCoordinator(
        int usuarios, int rondas, int intervaloRondasMs, int timeoutMs,
        Action<int>? onRondaLiberada = null, Action<int, double>? onRondaCompletada = null)
    {
        _usuarios = usuarios;
        _rondas = rondas;
        _intervaloRondasMs = intervaloRondasMs;
        _timeoutMs = timeoutMs;
        _onRondaLiberada = onRondaLiberada;
        _onRondaCompletada = onRondaCompletada;
        _r = new Ronda[rondas + 1]; // 1-based
        for (var i = 1; i <= rondas; i++)
        {
            _r[i] = new Ronda();
        }

        if (rondas >= 1)
        {
            _r[1].Permitida.TrySetResult(); // la primera ronda puede comenzar de inmediato
        }
    }

    /// <summary>true si una barrera agotó su timeout técnico (sincronización fallida).</summary>
    public bool TimeoutSincronizacion { get; private set; }

    /// <summary>true si la coordinación fue abortada (timeout o cancelación).</summary>
    public bool Faulted => _faulted;

    /// <summary>Número de ronda (1-based) de la siguiente participación de la instancia dada.</summary>
    public int ProximaRonda(int instancia) => _rondaPorInstancia.AddOrUpdate(instancia, 1, (_, v) => v + 1);

    /// <summary>Desfase de inicio máximo entre rondas, en ms (diagnóstico).</summary>
    public double DesfaseInicioMaximoMs => _skewPorRonda.Count == 0 ? 0 : _skewPorRonda.Max();

    /// <summary>Desfase de inicio promedio entre rondas, en ms (diagnóstico).</summary>
    public double DesfaseInicioPromedioMs => _skewPorRonda.Count == 0 ? 0 : _skewPorRonda.Average();

    public IReadOnlyList<double> SkewPorRonda => _skewPorRonda;

    /// <summary>Espera a que la ronda esté habilitada (la anterior terminó y pasó el intervalo).</summary>
    public async Task EsperarRondaPermitidaAsync(int ronda, CancellationToken cancellationToken)
    {
        if (_faulted) return;
        await EsperarAsync(_r[ronda].Permitida.Task, cancellationToken);
    }

    /// <summary>
    /// Barrera de inicio: registra la llegada del participante (distinto) y, cuando llegan los N, los libera
    /// a todos juntos. Lanza <see cref="TimeoutException"/> si no llegan los N dentro del timeout técnico.
    /// </summary>
    public async Task BarreraInicioAsync(int ronda, int instancia, CancellationToken cancellationToken)
    {
        if (_faulted) return;
        var r = _r[ronda];
        if (r.InicioLlegados.TryAdd(instancia, 0) && r.InicioLlegados.Count == _usuarios)
        {
            r.InicioLiberado.TrySetResult();
            _onRondaLiberada?.Invoke(ronda);
        }

        await EsperarAsync(r.InicioLiberado.Task, cancellationToken);
    }

    /// <summary>Registra el timestamp monotónico de inicio del envío HTTP del participante (para el desfase).</summary>
    public void RegistrarInicio(int ronda, int instancia, long timestamp) => _r[ronda].Timestamps[instancia] = timestamp;

    /// <summary>
    /// Barrera de fin: cada participante llega tras su GET. Cuando llegan los N, se calcula el desfase de la
    /// ronda, se aplica el intervalo entre rondas y se habilita la ronda siguiente.
    /// </summary>
    public async Task BarreraFinAsync(int ronda, int instancia, CancellationToken cancellationToken)
    {
        if (_faulted) return;
        var r = _r[ronda];
        if (r.FinLlegados.TryAdd(instancia, 0) && r.FinLlegados.Count == _usuarios)
        {
            RegistrarDesfase(ronda);

            // El intervalo solo ocurre ENTRE rondas: nunca después de la última (evita inflar la ventana).
            if (ronda < _rondas)
            {
                if (_intervaloRondasMs > 0)
                {
                    await Task.Delay(_intervaloRondasMs, cancellationToken);
                }

                _r[ronda + 1].Permitida.TrySetResult();
            }

            r.FinCompletado.TrySetResult();
        }

        await EsperarAsync(r.FinCompletado.Task, cancellationToken);
    }

    private void RegistrarDesfase(int ronda)
    {
        var ts = _r[ronda].Timestamps.Values.ToList();
        var skew = ts.Count < 2 ? 0.0 : (ts.Max() - ts.Min()) / (double)Stopwatch.Frequency * 1000.0;
        lock (_skewPorRonda) _skewPorRonda.Add(skew);
        _onRondaCompletada?.Invoke(ronda, skew);
    }

    /// <summary>Aborta la coordinación liberando todas las barreras (para que el runner se desenrede rápido).</summary>
    public void Abortar()
    {
        _faulted = true;
        foreach (var r in _r.Skip(1))
        {
            r.Permitida.TrySetResult();
            r.InicioLiberado.TrySetResult();
            r.FinCompletado.TrySetResult();
        }
    }

    private async Task EsperarAsync(Task espera, CancellationToken cancellationToken)
    {
        try
        {
            await espera.WaitAsync(TimeSpan.FromMilliseconds(_timeoutMs), cancellationToken);
        }
        catch (TimeoutException)
        {
            TimeoutSincronizacion = true;
            Abortar();
            throw;
        }
    }
}
