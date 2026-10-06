using System;
using System.Collections.Generic;
using UnityEngine;

// Oyuncunun YEREL görünüm tercihi (hangi deneme görünümü açık) — tek yer. Görünümler kendilerini buraya kaydeder
// (Register); ayarlar menüsündeki satır kayıtlı seçenekler arasında dolaşır. Aynı anda en çok bir görünüm seçilidir.
// Bu sınıf hiçbir görünümü tanımaz: bir görünümün klasörü silinirse seçeneği de kendiliğinden kaybolur.
// Ağdan gitmez, oynanışı etkilemez.
public static class LookPreference
{
    public readonly struct Option
    {
        public readonly string Id;
        public readonly string Label;

        public Option(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    // Hiçbir görünüm seçili değil (oyunun kendi görünümü).
    public const string Off = "";

    private const string PrefKey = "settings.look";

    private static readonly List<Option> RegisteredOptions = new();

    public static event Action Changed;

    public static IReadOnlyList<Option> Options => RegisteredOptions;

    // Oyuncu daha önce bir seçim yapmış mı (yapmadıysa görünümler kendi varsayılanını önerebilir).
    public static bool HasSavedSelection => PlayerPrefs.HasKey(PrefKey);

    // Seçili görünümün kimliği; kayıtlı olmayan bir kimlik (silinmiş görünüm) "kapalı" sayılır.
    public static string Selected
    {
        get
        {
            string saved = PlayerPrefs.GetString(PrefKey, Off);
            return IndexOf(saved) >= 0 ? saved : Off;
        }
        set
        {
            string id = value ?? Off;
            if (PlayerPrefs.GetString(PrefKey, Off) == id && HasSavedSelection)
                return;

            PlayerPrefs.SetString(PrefKey, id);
            Changed?.Invoke();
        }
    }

    public static bool IsSelected(string id) => Selected == id;

    public static void Register(string id, string label)
    {
        if (string.IsNullOrEmpty(id) || IndexOf(id) >= 0)
            return;

        RegisteredOptions.Add(new Option(id, label));
        Changed?.Invoke();
    }

    // Seçili görünümün adı; hiçbiri seçili değilse null.
    public static string SelectedLabel
    {
        get
        {
            int index = IndexOf(Selected);
            return index >= 0 ? RegisteredOptions[index].Label : null;
        }
    }

    // Sıradaki seçenek: kapalı → 1. görünüm → 2. görünüm → … → kapalı.
    public static void SelectNext()
    {
        int next = IndexOf(Selected) + 1;
        Selected = next < RegisteredOptions.Count ? RegisteredOptions[next].Id : Off;
    }

    private static int IndexOf(string id)
    {
        for (int i = 0; i < RegisteredOptions.Count; i++)
        {
            if (RegisteredOptions[i].Id == id)
                return i;
        }

        return -1;
    }

    // Domain reload kapalıyken Play'e yeniden girildiğinde önceki oturumun kayıtları kalmasın.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        RegisteredOptions.Clear();
        Changed = null;
    }
}
