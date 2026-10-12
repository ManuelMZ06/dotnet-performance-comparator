namespace DotNetPerformanceComparator.Application.Evaluations.Execution;

/// <summary>
/// Genera los niveles (escalones) de una prueba de Estrés escalonada (C-07.3): comienza en
/// <c>usuariosIniciales</c> y suma <c>incrementoUsuarios</c> hasta llegar a <c>usuariosMaximos</c>, que
/// siempre es el último nivel exacto (nunca se sobrepasa, sin duplicados, sin overflow). Función pura.
/// </summary>
public static class StressLevelPlan
{
    public static IReadOnlyList<int> Generar(int usuariosIniciales, int incrementoUsuarios, int usuariosMaximos)
    {
        var iniciales = Math.Max(1, usuariosIniciales);
        var incremento = Math.Max(1, incrementoUsuarios);

        if (usuariosMaximos <= iniciales)
        {
            // Las validaciones de M-04 impiden este caso; defensivamente, un único nivel al tope.
            return [usuariosMaximos > 0 ? usuariosMaximos : iniciales];
        }

        var niveles = new List<int>();
        long actual = iniciales; // long para evitar overflow al sumar el incremento
        while (actual < usuariosMaximos)
        {
            niveles.Add((int)actual);
            actual += incremento;
        }

        niveles.Add(usuariosMaximos); // el último nivel es exactamente el máximo
        return niveles;
    }
}
