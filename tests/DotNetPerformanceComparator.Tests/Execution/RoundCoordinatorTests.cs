using System.Collections.Concurrent;
using System.Diagnostics;
using DotNetPerformanceComparator.Application.Evaluations.Execution;

namespace DotNetPerformanceComparator.Tests.Execution;

public class RoundCoordinatorTests
{
    private const int Timeout = 2000;

    [Fact]
    public async Task BarreraInicio_LiberaSoloCuandoLleganLosN()
    {
        var coord = new RoundCoordinator(usuarios: 3, rondas: 1, intervaloRondasMs: 0, timeoutMs: Timeout);
        var t0 = coord.BarreraInicioAsync(1, 0, CancellationToken.None);
        var t1 = coord.BarreraInicioAsync(1, 1, CancellationToken.None);

        await Task.Delay(80);
        Assert.False(t0.IsCompleted); // con 2 de 3 no se libera
        Assert.False(t1.IsCompleted);

        var t2 = coord.BarreraInicioAsync(1, 2, CancellationToken.None);
        await Task.WhenAll(t0, t1, t2).WaitAsync(TimeSpan.FromSeconds(2)); // el tercero libera a todos
    }

    [Fact]
    public async Task ParticipanteDuplicado_NoCuentaDosVeces()
    {
        var coord = new RoundCoordinator(usuarios: 2, rondas: 1, intervaloRondasMs: 0, timeoutMs: Timeout);
        var a1 = coord.BarreraInicioAsync(1, 0, CancellationToken.None);
        var a2 = coord.BarreraInicioAsync(1, 0, CancellationToken.None); // misma instancia

        await Task.Delay(80);
        Assert.False(a1.IsCompleted); // sigue habiendo 1 participante distinto
        Assert.False(a2.IsCompleted);

        var b = coord.BarreraInicioAsync(1, 1, CancellationToken.None);
        await Task.WhenAll(a1, a2, b).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Timeout_LanzaYMarcaSincronizacionFallida()
    {
        var coord = new RoundCoordinator(usuarios: 2, rondas: 1, intervaloRondasMs: 0, timeoutMs: 120);
        await Assert.ThrowsAsync<TimeoutException>(() => coord.BarreraInicioAsync(1, 0, CancellationToken.None));
        Assert.True(coord.TimeoutSincronizacion);
        Assert.True(coord.Faulted);
    }

    [Fact]
    public async Task Cancelacion_LanzaOperationCanceled()
    {
        var coord = new RoundCoordinator(usuarios: 2, rondas: 1, intervaloRondasMs: 0, timeoutMs: Timeout);
        using var cts = new CancellationTokenSource();
        var t = coord.BarreraInicioAsync(1, 0, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
    }

    [Fact]
    public async Task Rondas_NSolicitudesPorRonda_SinAnticiparLaSiguiente()
    {
        const int n = 3, r = 2;
        var coord = new RoundCoordinator(n, r, intervaloRondasMs: 0, timeoutMs: Timeout);
        var eventos = new ConcurrentQueue<string>();
        var gets = 0;

        async Task Participar(int id)
        {
            for (var ronda = 1; ronda <= r; ronda++)
            {
                await coord.EsperarRondaPermitidaAsync(ronda, CancellationToken.None);
                await coord.BarreraInicioAsync(ronda, id, CancellationToken.None);
                eventos.Enqueue($"get:{ronda}");
                Interlocked.Increment(ref gets);
                coord.RegistrarInicio(ronda, id, Stopwatch.GetTimestamp());
                await coord.BarreraFinAsync(ronda, id, CancellationToken.None);
            }
        }

        await Task.WhenAll(Enumerable.Range(0, n).Select(Participar)).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(n * r, gets); // exactamente N·R solicitudes
        var lista = eventos.ToList();
        Assert.Equal(n, lista.Count(e => e == "get:1"));
        Assert.Equal(n, lista.Count(e => e == "get:2"));
        var ultimoR1 = lista.LastIndexOf("get:1");
        var primerR2 = lista.IndexOf("get:2");
        Assert.True(ultimoR1 < primerR2, "ninguna solicitud de la ronda 2 comienza antes de terminar la ronda 1");
    }

    [Fact]
    public async Task Intervalo_OcurreEntreRondas()
    {
        const int n = 2;
        var liberaciones = new ConcurrentDictionary<int, long>();
        var coord = new RoundCoordinator(n, rondas: 2, intervaloRondasMs: 200, timeoutMs: Timeout,
            onRondaLiberada: ronda => liberaciones[ronda] = Stopwatch.GetTimestamp());

        async Task Participar(int id)
        {
            for (var ronda = 1; ronda <= 2; ronda++)
            {
                await coord.EsperarRondaPermitidaAsync(ronda, CancellationToken.None);
                await coord.BarreraInicioAsync(ronda, id, CancellationToken.None);
                coord.RegistrarInicio(ronda, id, Stopwatch.GetTimestamp());
                await coord.BarreraFinAsync(ronda, id, CancellationToken.None);
            }
        }

        await Task.WhenAll(Participar(0), Participar(1)).WaitAsync(TimeSpan.FromSeconds(5));

        // La ronda 2 se libera después del EndBarrier de la ronda 1 + el intervalo (~200 ms).
        var ms = (liberaciones[2] - liberaciones[1]) / (double)Stopwatch.Frequency * 1000.0;
        Assert.True(ms >= 150, $"el intervalo entre rondas (~200 ms) debe aplicarse; medido {ms:0} ms");
    }

    [Fact]
    public async Task NoHayIntervaloDespuesDeLaUltimaRonda()
    {
        // Una sola ronda con intervalo grande: no debe aplicarse ningún intervalo (no hay ronda siguiente).
        const int n = 2;
        var coord = new RoundCoordinator(n, rondas: 1, intervaloRondasMs: 500, timeoutMs: Timeout);
        var reloj = Stopwatch.StartNew();

        async Task Participar(int id)
        {
            await coord.EsperarRondaPermitidaAsync(1, CancellationToken.None);
            await coord.BarreraInicioAsync(1, id, CancellationToken.None);
            coord.RegistrarInicio(1, id, Stopwatch.GetTimestamp());
            await coord.BarreraFinAsync(1, id, CancellationToken.None);
        }

        await Task.WhenAll(Participar(0), Participar(1)).WaitAsync(TimeSpan.FromSeconds(3));
        reloj.Stop();

        Assert.True(reloj.ElapsedMilliseconds < 300,
            $"no debe aplicarse el intervalo tras la última ronda; transcurrieron {reloj.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task IntervaloSoloEntreRondas_NUnoVeces()
    {
        // Con R rondas se aplican exactamente R-1 intervalos (entre rondas), nunca R.
        const int n = 2, r = 3, intervalo = 200;
        var coord = new RoundCoordinator(n, r, intervalo, timeoutMs: Timeout);
        var reloj = Stopwatch.StartNew();

        async Task Participar(int id)
        {
            for (var ronda = 1; ronda <= r; ronda++)
            {
                await coord.EsperarRondaPermitidaAsync(ronda, CancellationToken.None);
                await coord.BarreraInicioAsync(ronda, id, CancellationToken.None);
                coord.RegistrarInicio(ronda, id, Stopwatch.GetTimestamp());
                await coord.BarreraFinAsync(ronda, id, CancellationToken.None);
            }
        }

        await Task.WhenAll(Participar(0), Participar(1)).WaitAsync(TimeSpan.FromSeconds(5));
        reloj.Stop();

        // 2 intervalos (R-1) ≈ 400 ms; 3 intervalos serían ≈ 600 ms.
        Assert.InRange(reloj.ElapsedMilliseconds, 350, 580);
    }

    [Fact]
    public async Task StartSkew_SeCalculaConRelojMonotono()
    {
        var coord = new RoundCoordinator(usuarios: 2, rondas: 1, intervaloRondasMs: 0, timeoutMs: Timeout);
        var t0 = coord.BarreraInicioAsync(1, 0, CancellationToken.None);
        var t1 = coord.BarreraInicioAsync(1, 1, CancellationToken.None);
        await Task.WhenAll(t0, t1).WaitAsync(TimeSpan.FromSeconds(2));

        var baseTs = Stopwatch.GetTimestamp();
        var delta = Stopwatch.Frequency / 100; // ~10 ms en ticks
        coord.RegistrarInicio(1, 0, baseTs);
        coord.RegistrarInicio(1, 1, baseTs + delta);

        await Task.WhenAll(coord.BarreraFinAsync(1, 0, CancellationToken.None), coord.BarreraFinAsync(1, 1, CancellationToken.None))
            .WaitAsync(TimeSpan.FromSeconds(2));

        Assert.InRange(coord.DesfaseInicioMaximoMs, 9.0, 11.0);
        Assert.InRange(coord.DesfaseInicioPromedioMs, 9.0, 11.0);
    }
}
