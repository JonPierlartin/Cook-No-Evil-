using System;

// CNE Toon görünümünün açık/kapalı durumu — tek yer. Görünüm seçici (ayarlar) ve test sahnesi bunu yazar;
// post-process, outline ve materyal uygulayıcıları buradan okur. Oyuncunun YEREL görünüm tercihidir; ağdan gitmez,
// oynanışı etkilemez.
public static class CNELook
{
    private static bool _active;

    public static event Action Changed;

    public static bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
                return;

            _active = value;
            Changed?.Invoke();
        }
    }

    // Domain reload kapalıyken Play'e yeniden girildiğinde önceki oturumun değeri kalmasın.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _active = false;
        Changed = null;
    }
}
