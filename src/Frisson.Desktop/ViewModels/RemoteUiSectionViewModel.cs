using System.Collections.ObjectModel;

namespace Frisson.Desktop.ViewModels;

/// <summary>
/// Groups Remote UI items under an optional group title.
/// Items before the first group declaration belong to a null-title section (no border).
/// </summary>
public class RemoteUiSectionViewModel
{
    /// <summary>Group title, null for top-level items.</summary>
    public string? Title { get; set; }

    /// <summary>Group key from declaration (for template matching).</summary>
    public string? Key { get; set; }

    /// <summary>Whether this section has a group title.</summary>
    public bool IsGroup => Title != null;

    public ObservableCollection<RemoteUiItemViewModel> Items { get; } = new();
}
