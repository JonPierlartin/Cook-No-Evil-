using System;
using Unity.Netcode;
using UnityEngine;

// Kasiyer VE Yamak'in emote carkindan sectigi tepki (GDD 2.2 — Kasiyer'in Yamak'i
// yonlendirmesi; Yamak'in erisimi kisitli/placeholder — bkz. yamakEmoteLimit). Eskiden
// sadece Yamak'a hedefli bir ClientRpc'ydi (ReceivedEmoteIcon adinda ayri bir UI ile);
// yeniden tasarlandi: artik HERKESE broadcast ediliyor ve secimi yapan oyuncunun kendi
// karakteri uzerinde (PlayerEmoteReactor) herkesin gorebilecegi kisa bir gorsel tepki
// tetikliyor. NetworkVariable degil bilerek ClientRpc kullaniliyor — ayni emote art arda
// iki kez secilirse bir NetworkVariable'da deger degismedigi icin OnValueChanged hic
// tetiklenmezdi (sessizce yutulurdu); RPC her cagriyi kosulsuz iletir.
[RequireComponent(typeof(NetworkObject))]
public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance { get; private set; }

    [SerializeField] private EmoteDefinition[] availableEmotes;

    // Yamak da carka erisebilir ama Kasiyer'den daha kisitli bir secimle: sadece
    // availableEmotes dizisinin ILK N elemani. Placeholder/basit tutuluyor (kullanici
    // istegi) — gercek kisitli-liste icerigi (hangi emote'lar) ileride ayrica
    // tasarlanacak, simdilik sadece SAYI kisitlanmis durumda.
    [SerializeField] private int yamakEmoteLimit = 1;

    // Ayni veya farkli emote farketmeksizin, son basarili secimden itibaren bu sure
    // gecmeden yeni bir secim reddedilir (spam/iletisim kirliligini onlemek icin,
    // kullanici istegi). GLOBAL bir cooldown — kimin sectigi onemli degil, herkes
    // icin ayni sayaci paylasir (yamakEmoteLimit gibi basit tutuluyor, kisi-basi
    // ayrica takip edilmiyor).
    [SerializeField] private float selectionCooldown = 2.5f;

    // Server-authoritative zaman damgasi (NetworkManager.ServerTime.Time, sunucuda
    // yazilir) — client'lar IsOnCooldown uzerinden canli okuyup carki acmadan/secim
    // yapmadan once kendi taraflarinda da kontrol edebilir (yamakEmoteLimit'teki gibi
    // hem client hem server tarafinda).
    private readonly NetworkVariable<double> _lastSelectionServerTime =
        new(-1000d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // (kasiyerClientId, emoteIndex) — PlayerEmoteReactor kendi OwnerClientId'siyle
    // karsilastirip sadece dogru objede tepki oynatir. Isim tarihsel: artik Yamak da
    // tetikleyebiliyor, ama alan/parametre adi degistirilmedi (RPC/UI'da hala
    // "hangi client tetikledi" anlaminda kullaniliyor).
    public event Action<ulong, int> OnEmoteTriggered;

    public EmoteDefinition[] AvailableEmotes => availableEmotes;
    public int YamakEmoteLimit => yamakEmoteLimit;
    public float SelectionCooldown => selectionCooldown;

    public bool IsOnCooldown =>
        NetworkManager != null &&
        NetworkManager.ServerTime.Time - _lastSelectionServerTime.Value < selectionCooldown;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectEmoteServerRpc(int emoteIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length)
            return;

        if (RoleManager.Instance == null)
            return;

        var role = RoleManager.Instance.GetRole(senderId);
        if (role != PlayerRole.Kasiyer && role != PlayerRole.Yamak)
            return;

        // Yamak sadece kisitli (ilk N) emote'a erisebilir; Kasiyer tam listeyi kullanir.
        if (role == PlayerRole.Yamak && emoteIndex >= yamakEmoteLimit)
            return;

        if (!RoleManager.Instance.IsRoundActive.Value)
            return;

        // "Oyun durduruldu" (bkz. PlayerController/PlayerInteractor/EmoteWheelUI ayni
        // kontrol) server-authoritative olarak burada da doğrulanıyor — client tarafi
        // (EmoteWheelUI) carki acmayi zaten engelliyor, bu sadece bypass'a karsi savunma.
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused.Value)
            return;

        // Ayni/farkli emote farketmeksizin, cooldown suresi dolmadan yeni secim
        // server-authoritative olarak reddedilir — client tarafi (EmoteWheelUI) zaten
        // ayni kontrolu yapip carki acmiyor/secim yollamiyor, bu bypass'a karsi savunma.
        if (IsOnCooldown)
            return;

        _lastSelectionServerTime.Value = NetworkManager.ServerTime.Time;

        EmoteTriggeredClientRpc(senderId, emoteIndex);
    }

    [ClientRpc]
    private void EmoteTriggeredClientRpc(ulong kasiyerClientId, int emoteIndex)
    {
        OnEmoteTriggered?.Invoke(kasiyerClientId, emoteIndex);
    }
}
