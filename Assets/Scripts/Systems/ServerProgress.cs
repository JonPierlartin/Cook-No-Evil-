using Unity.Netcode;
using UnityEngine;

// K5 (CLAUDE.md): "ilerleme verisi nesnenin ustunde durur, makinede degil" — TEK ortak temel
// bilesen; izgara, fritoz, icecek, dondurma ve yangin sondurme HEPSI bunu kullanmalidir (K5).
// BILEREK Item'dan TUREMEZ: yangin tupu gibi oge OLMAYAN nesnelere de (orn. ileride ızgara
// yuvasinin kendisine degil, sondurulecek ates nesnesine) takilabilmelidir.
//
// Tasarim: bir ProgressProfile sirali FAZLAR tanimlar (her fazin kendi suresi, saniye). Sunucu
// tek bir fonksiyonla (ServerAdvance) ilerletilir; "isActive" parametresi decay'i de kapsar —
// ayri bir "geri say" fonksiyonu YOK, ayni cagrinin bir parametresi (GDD 5.2.3: "Gerekli sure ve
// geri sayma hizi ayri tunable parametrelerdir" — decayRatePerSecond, ilerleme hizindan (sabit
// 1 birim/sn) BAGIMSIZ ayri bir alan).
//
// Ucuncu tuketici modeli (yalnizca TASARIM, bu adimda yazilmadi): icecek dolumu (GDD 6.7.1) TEK
// fazli bir profille (orn. "Dolu", sure = dolum suresi) temsil edilebilir; decayEnabled=false
// (yariken alinirsa ilerleme SABIT kalir — "%32'de alindiysa %32 dolu kalir"), ve "tepe noktaya
// ulasinca otomatik durur" kurali zaten TERMINAL FAZ kirpma davranisiyla (asagida) bedava gelir —
// icecek makinesi yalnizca ServerAdvance(dt, isActive:true)'i dugmeye basilinca-doldukca cagirir,
// bu sinifin kendisi degismez.
[RequireComponent(typeof(NetworkObject))]
public class ServerProgress : NetworkBehaviour
{
    [Tooltip("Sirali faz/sure verisi (GDD 5.2.1). Kofte icin: Cig -> Pismis -> Yanmis.")]
    [SerializeField] private ProgressProfile profile;

    [Tooltip("Acikken, ServerAdvance(dt, isActive:false) ilerlemeyi GERI SAYDIRIR (GDD 5.2.3 " +
        "yangin sondurme). Kapaliyken isActive:false hicbir sey yapmaz (orn. icecek/dondurma " +
        "dolumu — GDD 6.7.1/6.7.2: birakinca ilerleme SABIT kalir, azalmaz). Varsayilan KAPALI.")]
    [SerializeField] private bool decayEnabled;

    [Tooltip("Decay acikken saniyede kac birim (saniye) geri sayacagi. Ilerleme hizindan (sabit " +
        "1 birim/sn) BAGIMSIZ ayri bir tunable (GDD 5.2.3: 'gerekli sure ve geri sayma hizi ayri " +
        "tunable parametrelerdir').")]
    [SerializeField, Min(0f)] private float decayRatePerSecond = 1f;

    // Sunucu sahipli faz indeksi (ProgressProfile.Phases'e index) ve o faz icindeki ilerleme
    // (saniye, 0..fazin Duration'i). Ikisi de NetworkVariable — K6 geregi yalnizca sunucu yazar.
    public readonly NetworkVariable<int> PhaseIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> Progress =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public ProgressProfile Profile => profile;

    public bool IsAtFinalPhase => profile != null && profile.Phases.Count > 0
        && PhaseIndex.Value >= profile.Phases.Count - 1;

    // Mevcut fazin suresi (saniye). Profil/faz yoksa 0 (cagiran taraf bunu "ilerleme anlamsiz"
    // olarak okumali).
    public float CurrentPhaseDuration
    {
        get
        {
            if (profile == null || profile.Phases.Count == 0)
                return 0f;

            int index = Mathf.Clamp(PhaseIndex.Value, 0, profile.Phases.Count - 1);
            return profile.Phases[index].Duration;
        }
    }

    // UI icin (orn. ileride icecek doluluk cubugu): mevcut faz icinde 0..1 arasi kesir.
    public float NormalizedProgress
    {
        get
        {
            float duration = CurrentPhaseDuration;
            return duration > 0f ? Mathf.Clamp01(Progress.Value / duration) : 0f;
        }
    }

    // Sunucunun TEK ilerletme fonksiyonu. isActive=true iken ilerler (dt kadar); isActive=false
    // iken YALNIZCA decayEnabled acikken decayRatePerSecond hiziyla geri sayar, aksi halde no-op.
    // Son fazda (terminal) ilerleme o fazin suresine KIRPILIR ve oradan OTESINE GECMEZ ("ilerleme
    // orada durur" — GDD hem "Yanmis" hem icecegin "tepe noktasi" icin ayni kurali istiyor).
    public void ServerAdvance(float deltaTime, bool isActive = true)
    {
        if (!IsServer)
            return;

        if (profile == null || profile.Phases.Count == 0)
            return;

        float delta = isActive ? deltaTime : (decayEnabled ? -decayRatePerSecond * deltaTime : 0f);
        if (delta == 0f)
            return;

        float duration = CurrentPhaseDuration;
        float newProgress = Progress.Value + delta;

        if (IsAtFinalPhase)
        {
            Progress.Value = Mathf.Clamp(newProgress, 0f, duration);
            return;
        }

        if (newProgress >= duration)
        {
            // Sonraki faza gec; asan miktari TASI (kucuk dt adimlarinda ihmal edilebilir ama
            // zaman kaybini onler).
            PhaseIndex.Value++;
            Progress.Value = Mathf.Max(0f, newProgress - duration);
            return;
        }

        Progress.Value = Mathf.Max(0f, newProgress);
    }

    // Yeni doganda/geri alindiginda sifirlamak icin (bu adimda hicbir yerden cagrilmiyor — kofte
    // zaten Cig/0 doguyor, NetworkVariable varsayilanlariyla).
    public void ServerReset()
    {
        if (!IsServer)
            return;

        PhaseIndex.Value = 0;
        Progress.Value = 0f;
    }
}
