using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// IVoiceProvider ile calisan, AudioSource entegreli, rol tabanli sesli sohbet yonetimi (GDD 10.4):
//  - Kasiyer (dilsiz): mikrofonu sunucu tarafinda susturulur (paketi hic relay edilmez).
//  - Komi (sagir): HICBIR oyuncunun sesini duymaz — Faz 0 karari (GDD 11.2). Sef->Komi gibberish Faz 0.5.
//    "Sagir mi" karari DeafHearing'dedir (tek yer). Gelen paket hic cozulmez/calinmaz: ses
//    VoiceStreamPlayer'da OnAudioFilterRead ile enjekte edildigi icin AudioSource.mute'a guvenilmez
//    (30 Eyl testi: mute ile Komi oyunculari net duyuyordu). Ana ses seviyesinden bagimsizdir (K3).
//    Oyun efektleri (GDD 4.1.3) DeafHearing + RoleAwareAudioRange ile ayrica bogulur/daralir.
// Bu kisitlamalar yalnizca round aktifken uygulanir; lobide herkes normal konusup duyabilir.
// Mekansal ses (K4, GDD 10.4): round sirasinda her paketin seviyesi ve yonu, konusanin karakteri ile yerel
//    dinleyici arasindaki SES YOLUNDAN hesaplanir (AcousticSpace: ses duvardan ve kapali kapidan gecmez,
//    pencerelerden dolasir). Seviye ve yon VoiceStreamPlayer'da orneklere uygulanir; hoparlor AudioSource'u 2B'dir
//    (ses filtreyle enjekte edildigi icin Unity'nin 3B zayiflamasina guvenilmez). Lobide mekansal ses yoktur.
[RequireComponent(typeof(NetworkObject))]
public class VoIPController : NetworkBehaviour
{
    private IVoiceProvider _voiceProvider;
    private readonly Dictionary<ulong, VoiceStreamPlayer> _speakerPlayers = new();
    // Konusanin karakteri (ses yolunun baslangici). Karakter round'da dogar; bulunamazsa mekansal ses uygulanmaz.
    private readonly Dictionary<ulong, PlayerController> _speakerCharacters = new();
    private PlayerRole _localRole = PlayerRole.None;

    // Mikrofon seviyesi pakette tek bayt olarak gider (yüzde: 100 = olduğu gibi). Steam sesi sıkıştırılmış verir,
    // gönderen tarafta ölçeklenemez; seviye alıcıda, çözülmüş örneklere uygulanır (VoiceStreamPlayer.Gain).
    private const float MicGainScale = 100f;

    private static bool IsRoundActive => GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;

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

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged += HandleRoundStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned -= HandleLocalRoleAssigned;

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged -= HandleRoundStateChanged;

        _voiceProvider?.Shutdown();
        _voiceProvider = null;

        // VoIPController da RoleManager gibi GameSystems (sahne-ici, kalici) uzerinde yasiyor —
        // dinamik olarak olusturulan konusmaci AudioSource'lari (VoiceSpeaker_X) burada
        // Destroy edilmezse bir sonraki oturuma sizar: hem eski GameObject'ler sahnede
        // birikir hem deayni clientId yeniden kullanilirsa (StartHost sonrasi ayni makine
        // her zaman clientId 0 olur) GetOrCreateSpeakerPlayer eski/yanlis rol icin
        // kurulmus bir AudioSource'u geri dondurur.
        foreach (var player in _speakerPlayers.Values)
        {
            if (player != null)
                Destroy(player.gameObject);
        }
        _speakerPlayers.Clear();
        _speakerCharacters.Clear();

        _localRole = PlayerRole.None;
    }

    private void Update()
    {
        if (_voiceProvider == null || !IsSpawned)
            return;

        _voiceProvider.Tick();

        if (_voiceProvider.ShouldTransmitLocalVoice && _voiceProvider.TryReadLocalVoicePacket(out var packet))
            SendVoiceServerRpc(packet, (byte)Mathf.RoundToInt(GameSettings.MicGain * MicGainScale));
    }

    private void HandleLocalRoleAssigned(PlayerRole role)
    {
        _localRole = role;
        UpdateLocalMuteState();

        // Rol hoparlorler olustuktan sonra gelebilir (rejoin): mevcut hoparlorler yeni role gore kurulur.
        foreach (var player in _speakerPlayers.Values)
            ApplyRoleBasedAudioSettings(player.Source);
    }

    private void HandleRoundStateChanged(RoundState previous, RoundState current)
    {
        UpdateLocalMuteState();

        // Round durumu degistiginde mevcut hoparlorlerin (zaten olusturulmus AudioSource'lar)
        // filtrelerini de guncelliyoruz; yoksa round baslamadan once konusan biri icin
        // olusturulmus hoparlor, round basladiktan sonra da kisitlamasiz kalirdi.
        foreach (var player in _speakerPlayers.Values)
            ApplyRoleBasedAudioSettings(player.Source);
    }

    private void UpdateLocalMuteState()
    {
        // Kasiyer'in mikrofonu SADECE round aktifken susturulur; lobide herkes normal
        // konusabilir. Lokal yakalamayi da kapatiyoruz; asil yetki asagidaki
        // SendVoiceServerRpc icindeki server-side kontroldedir (istemci taraf hileyle
        // tekrar acsa bile server round aktifken Kasiyer'in paketini asla relay etmez).
        bool shouldMute = _localRole == PlayerRole.Kasiyer && IsRoundActive;
        _voiceProvider?.SetLocalCaptureMuted(shouldMute);
    }

    // GameSystems sunucu tarafindan (host) sahiplenilen sahne-ici bir NetworkObject;
    // her client kendi sesini gonderebilmeli, sadece "owner" degil — bu yuzden
    // RequireOwnership = false gerekiyor (varsayilaninda "Only the owner can invoke..." hatasi verir).
    [ServerRpc(RequireOwnership = false)]
    private void SendVoiceServerRpc(byte[] compressedData, byte micGainPercent, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (IsRoundActive && RoleManager.Instance != null && RoleManager.Instance.GetRole(senderId) == PlayerRole.Kasiyer)
            return;

        ReceiveVoiceClientRpc(senderId, compressedData, micGainPercent);
    }

    [ClientRpc]
    private void ReceiveVoiceClientRpc(ulong senderId, byte[] compressedData, byte micGainPercent)
    {
        if (senderId == NetworkManager.Singleton.LocalClientId)
            return;

        // Sagir dinleyici hicbir oyuncunun sesini duymaz: paket cozulmez, hoparlore yazilmaz (bkz. dosya basi).
        if (DeafHearing.IsLocalListenerDeaf)
            return;

        var player = GetOrCreateSpeakerPlayer(senderId);
        // Konuşanın mikrofon seviyesi × bu oyuncunun "sesli sohbet" ayarı (yerel) × mekânın zayıflatması.
        float spatialGain = ComputeSpatialGain(senderId, out float pan);
        player.Gain = micGainPercent / MicGainScale * GameSettings.VoiceVolume * spatialGain;
        player.Pan = pan;
        if (spatialGain <= 0f)
            return;

        _voiceProvider.DecompressAndEnqueue(player.Source, compressedData);
    }

    // Round sırasında: ses yolunun etkin mesafesinden seviye, sesin dinleyiciye geldiği noktadan yön.
    // Lobide, ya da konuşanın karakteri / dinleyici / akustik veri yoksa: tam seviye, ortadan.
    private float ComputeSpatialGain(ulong speakerId, out float pan)
    {
        pan = 0f;
        var space = AcousticSpace.Instance;
        var listener = AcousticSpace.Listener;
        if (!IsRoundActive || space == null || listener == null)
            return 1f;

        var speaker = FindSpeakerCharacter(speakerId);
        if (speaker == null)
            return 1f;

        float gain = space.VoiceGain(speaker.transform.position, listener.position, out var apparent);
        var toSource = apparent - listener.position;
        toSource.y = 0f;
        if (toSource.sqrMagnitude > 0.0001f)
            pan = Vector3.Dot(toSource.normalized, listener.right);

        return gain;
    }

    private PlayerController FindSpeakerCharacter(ulong speakerId)
    {
        if (_speakerCharacters.TryGetValue(speakerId, out var cached) && cached != null && cached.OwnerClientId == speakerId)
            return cached;

        foreach (var character in FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude))
        {
            if (character.OwnerClientId != speakerId)
                continue;

            _speakerCharacters[speakerId] = character;
            return character;
        }

        _speakerCharacters.Remove(speakerId);
        return null;
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
        // Hoparlör 2B'dir: mesafe ve yön ComputeSpatialGain'de hesaplanıp örneklere uygulanır (bkz. dosya başı).
        // (Komi'nin sağırlığı burada DEĞİL, ReceiveVoiceClientRpc'de paket düzeyinde uygulanır.)
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }
}
