namespace DotNetPerformanceComparator.Domain.Constants;

/// <summary>
/// Nombres de los tipos de prueba del catálogo (sembrados), usados para asociar cada tipo con
/// su conjunto de parámetros. Coinciden con <c>TipoPrueba.Nombre</c>.
/// </summary>
public static class TiposPruebaConocidos
{
    public const string Carga = "Carga";
    public const string Estres = "Estrés";
    public const string Concurrencia = "Concurrencia";
}
