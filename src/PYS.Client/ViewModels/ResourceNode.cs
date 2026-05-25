using CommunityToolkit.Mvvm.ComponentModel;
using PYS.Client.Models;

namespace PYS.Client.ViewModels;

/// <summary>
/// Görev detayındaki bağlı kaynak ağacının düz-liste düğümü. Klasörler açılınca
/// çocukları bu listeye girintili olarak eklenir/çıkarılır.
/// </summary>
public sealed partial class ResourceNode : ObservableObject
{
    public ProjectResourceItem Item { get; }
    public int Indent { get; }

    /// <summary>indent==0: göreve doğrudan bağlı (görevden kaldırılabilir).</summary>
    public bool CanUnlink => Indent == 0;

    public Thickness IndentMargin => new(Indent * 16, 0, 0, 0);

    [ObservableProperty]
    private bool _isExpanded;

    public ResourceNode(ProjectResourceItem item, int indent)
    {
        Item = item;
        Indent = indent;
    }
}
