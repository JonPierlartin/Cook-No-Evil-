using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// IVoiceProvider ile calisan, AudioSource entegreli, rol tabanli sesli sohbet yonetimi.
// GDD 2.2 / Red Line 2: Kasiyer'in mikrofonu server tarafindan susturulur, Yamak gelen
// ses sohbetini Low-Pass filtreli duyar, Sef'in etkilesim/VoIP sesi Hyper-Spatial olur.
[RequireComponent(typeof(NetworkObject))]
public class VoIPController : NetworkBehaviour
{
    [Tooltip("Yamak (Sagir) rolu icin gelen ses sohbetine uygulanacak Low-Pass kesim frekansi (Hz).")]
    [SerializeField] private float yamakLowPassCutoffHz = 800f;

    private IVoiceProvider _voiceProvider;
    private readonly Dictionary<ulong, VoiceStreamPlayer> _speakerPlayers = new();
    private PlayerRole _localRole = PlayerRole.None;

    public override void OnNetworkSpawn()
    {
        var mode = NetworkTransportManager.Instance != null
            ? NetworkTransportManager.Instance.CurrentMode
            : TransportMode.Steam;

        _voiceProvider = mode == TransportMode.LocalUdp
            ? new MockVoiceProvider()
            : new SteamworksVoiceProvider();

        _voiceProvider.Initialize();

        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned += HandleLocalRoleAssigned;
    }

    public override void OnNetworkDespawn()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned -= HandleLocalRoleAssigned;

        _voiceProvider?.Shutdown();
        _voiceProvider = null;
    }

    private void Update()
    {
        if (_voiceProvider == null || !IsSpawned)
            return;

        _voiceProvider.Tick();

        if (_voiceProvider.ShouldTransmitLocalVoice && _voiceProvider.TryReadLocalVoicePacket(out var packet))
            SendVoiceServerRpc(packet);
    }

    private void HandleLocalRoleAssigned(PlayerRole role)
    {
        _localRole = role;

        // Kasiyer'in mikrofonu susturulur. Lokal yakalamayi da kapatiyoruz; asil yetki
        // asagidaki SendVoiceServerRpc icindeki server-side rol kontrolundedir (istemci
        // taraf hileyle tekrar acsa bile server Kasiyer'in paketini asla relay etmez).
        _voiceProvider?.SetLocalCaptureMuted(role == PlayerRole.Kasiyer);
    }

    // GameSystems sunucu tarafindan (host) sahiplenilen sahne-ici bir NetworkObject;
    // her client kendi sesini gonderebilmeli, sadece "owner" degil — bu yuzden
    // RequireOwnership = false gerekiyor (varsayilaninda "Only the owner can invoke..." hatasi verir).
    [ServerRpc(RequireOwnership = false)]
    private void SendVoiceServerRpc(byte[] compressedData, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance != null && RoleManager.Instance.GetRole(senderId) == PlayerRole.Kasiyer)
            return;

        ReceiveVoiceClientRpc(senderId, compressedData);
    }

    [ClientRpc]
    private void ReceiveVoiceClientRpc(ulong senderId, byte[] compressedData)
    {
        if (senderId == NetworkManager.Singleton.LocalClientId)
            return;

        var player = GetOrCreateSpeakerPlayer(senderId);
        _voiceProvider.DecompressAndEnqueue(player.Source, compressedData);
    }

    private VoiceStreamPlayer GetOrCreateSpeakerPlayer(ulong speakerId)
    {
        if (_speakerPlayers.TryGetValue(speakerId, out var existing))
            return existing;

        var speakerObject = new GameObject($"VoiceSpeaker_{speakerId}");
        speakerObject.transform.SetParent(transform);

        var source = speakerObject.AddComponent<AudioSource>();
        ApplyRoleBasedAudioSettings(source);

        var player = speakerObject.AddComponent<VoiceStreamPlayer>();
        player.Source = source;

        _voiceProvider.ConfigureRemoteSpeaker(speakerObject, source);

        _speakerPlayers[speakerId] = player;
        return player;
    }

    private void ApplyRoleBasedAudioSettings(AudioSource source)
    {
        switch (_localRole)
        {
            case PlayerRole.Yamak:
                // Sagir icin gelen ses sohbeti tamamen bogukluyor.
                source.spatialBlend = 0f;
                var lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = yamakLowPassCutoffHz;
                break;

            case PlayerRole.Sef:
                // Kor icin abartili 3D Uzamsal Ses (Hyper-Spatial Audio).
                source.spatialBlend = 1f;
                source.spread = 0f;
                source.dopplerLevel = 1f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.maxDistance = 25f;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, 25f, 0f));
                break;

            default:
                source.spatialBlend = 0.5f;
                break;
        }
    }
}
