namespace DotNetPerformanceComparator.Application.Projects.Incorporation;

public sealed record IncorporarProyectoRequest(string? Nombre, string? RutaProyecto);

public sealed record IncorporarProyectoResult(bool Exito, string Mensaje, int? ProyectoId = null)
{
    public static IncorporarProyectoResult Fallo(string mensaje) => new(false, mensaje);
}
