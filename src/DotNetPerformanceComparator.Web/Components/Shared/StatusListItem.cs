namespace DotNetPerformanceComparator.Web.Components.Shared;

public enum StatusListState
{
    Pendiente,
    Correcto,
    Error
}

public sealed record StatusListItem(string Text, StatusListState State);
