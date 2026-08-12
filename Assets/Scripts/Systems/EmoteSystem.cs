using System;
using Unity.Netcode;
using UnityEngine;

// Kasiyer'in emote carkindan sectigi tepki (GDD 2.2 — Kasiyer'in Yamak'i yonlendirmesi).
// Eskiden sadece Yamak'a hedefli bir ClientRpc'ydi (ReceivedEmoteIcon adinda ayri bir
// UI ile); yeniden tasarlandi: artik HERKESE broadcast ediliyor ve Kasiyer'in kendi
// karakteri uzerinde (PlayerEmoteReactor) herkesin gorebilecegi kisa bir gorsel tepki
// tetikliyor. NetworkVariable degil bilerek ClientRpc kullaniliyor — Kasiyer ayni
// emote'u art arda iki kez secerse bir NetworkVariable'da deger degismedigi icin
// OnValueChanged hic tetiklenmezdi (sessizce yutulurdu); RPC her cagriyi kosulsuz iletir.
[RequireComponent(typeof(NetworkObject))]
public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance { get; private set; }

    [SerializeField] private EmoteDefinition[] availableEmotes;

    // (kasiyerClientId, emoteIndex) — PlayerEmoteReactor kendi OwnerClientId'siyle
    // karsilastirip sadece dogru objede tepki oynatir.
    public event Action<ulong, int> OnEmoteTriggered;

    public EmoteDefinition[] AvailableEmotes => availableEmotes;

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

        if (RoleManager.Instance == null || RoleManager.Instance.GetRole(senderId) != PlayerRole.Kasiyer)
            return;

        if (!RoleManager.Instance.IsRoundActive.Value)
            return;

        EmoteTriggeredClientRpc(senderId, emoteIndex);
    }

    [ClientRpc]
    private void EmoteTriggeredClientRpc(ulong kasiyerClientId, int emoteIndex)
    {
        OnEmoteTriggered?.Invoke(kasiyerClientId, emoteIndex);
    }
}
