using System;
using Unity.Netcode;
using UnityEngine;

// Kasiyer'in Yamak'a emote ile iletisim kurmasi (GDD 2.2). Bu, "state blindness"
// veri gizleme kurali (Red Line 1) DEGIL — o kural cooking-state'e ozgu, burada
// uygulanmiyor; bu sadece rol-bazli bir mesajlasma eylemi, hedefli ClientRpc
// kullanmak network-verimliligi acisindan normal/uygun bir tercih.
[RequireComponent(typeof(NetworkObject))]
public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance { get; private set; }

    [SerializeField] private EmoteDefinition[] availableEmotes;

    public event Action<int> OnEmoteReceived;

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

        if (!TryGetClientIdForRole(PlayerRole.Yamak, out ulong yamakClientId))
            return;

        var targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { yamakClientId } }
        };

        EmoteReceivedClientRpc(emoteIndex, targetParams);
    }

    [ClientRpc]
    private void EmoteReceivedClientRpc(int emoteIndex, ClientRpcParams rpcParams = default)
    {
        OnEmoteReceived?.Invoke(emoteIndex);
    }

    private bool TryGetClientIdForRole(PlayerRole role, out ulong clientId)
    {
        clientId = 0;

        if (RoleManager.Instance == null || NetworkManager == null)
            return false;

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            if (RoleManager.Instance.GetRole(id) != role)
                continue;

            clientId = id;
            return true;
        }

        return false;
    }
}
