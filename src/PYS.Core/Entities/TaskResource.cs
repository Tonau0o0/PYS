using PYS.Core.Common;

namespace PYS.Core.Entities;

/// <summary>
/// Görev ile proje kaynağı arasındaki çok-çok bağ. Bir kaynak (dosya/klasör/video)
/// birden çok göreve bağlanabilir; göreve bağlı kaynaklar tüm proje üyelerince görülür.
/// </summary>
public class TaskResource : BaseEntity
{
    public int TaskId { get; set; }
    public ProjectTask? Task { get; set; }

    public int ResourceId { get; set; }
    public ProjectResource? Resource { get; set; }
}
