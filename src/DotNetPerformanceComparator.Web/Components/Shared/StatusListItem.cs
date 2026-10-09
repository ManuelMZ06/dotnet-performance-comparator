namespace DotNetPerformanceComparator.Web.Components.Shared;

public enum StatusListState
{
    Pendiente,
    EnProceso,
    Correcto,
    Error
}

public sealed record StatusListItem(string Text, StatusListState State);
