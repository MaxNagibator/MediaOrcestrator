using MediaOrcestrator.Domain;

namespace MediaOrcestrator.Runner.MediaContextMenu;

public sealed record MediaSelection(IReadOnlyList<Media> Items, Source? SpecificSource)
{
    public int Count => Items.Count;
    public bool IsBatch => Items.Count > 1;
    public Media First => Items[0];

    public IReadOnlyList<Media> InSelectionOrder { get; init; } = Items;
    public IReadOnlyList<Source>? GridSources { get; init; }
}
