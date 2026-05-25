namespace PYS.Client.Services;

/// <summary>Kullanıcıya indirme hedef klasörünü seçtiren platform servisi.</summary>
public interface IFolderPicker
{
    /// <summary>Bir klasör seçtirir; iptal edilirse null döner.</summary>
    Task<string?> PickFolderAsync();
}
