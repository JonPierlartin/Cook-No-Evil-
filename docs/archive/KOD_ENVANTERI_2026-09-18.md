# Cook No Evil! — Tam Kod Envanteri (Referans Dökümü)

> Bu belge bir özet/değerlendirme DEĞİLDİR. Sadece mevcut kodun/sahnenin/asset'lerin
> okunmasıyla elde edilen kesin isim ve imzaları içerir. Emin olunamayan yerler
> "tespit edilemedi" olarak işaretlenmiştir.

---

## 1. DOSYA AĞACI

### Assets/Scripts (29 .cs dosyası)
Assets/Scripts/Core/
BurgerRecipe.cs
EmoteDefinition.cs
IngredientType.cs
InteractionType.cs
IRoleAssignmentStrategy.cs
IVoiceProvider.cs
PlayerRole.cs
RoundState.cs
SequentialRoleAssignmentStrategy.cs
TransportMode.cs

Assets/Scripts/Network/
MockVoiceProvider.cs
NetworkTransportManager.cs
PlayerSpawner.cs
RoleManager.cs
SteamLobbyManager.cs
SteamworksVoiceProvider.cs
VoiceStreamPlayer.cs
VoIPController.cs

Assets/Scripts/Player/
PlayerController.cs
PlayerEmoteReactor.cs
PlayerInteractor.cs
PlayerInventory.cs

Assets/Scripts/Systems/
BurgerAssemblyStation.cs
EmoteSystem.cs
GameLoopManager.cs
HoldOrPressInteractable.cs

Assets/Scripts/UI/
EmoteWheelUI.cs
HotbarUI.cs
InteractionToastUI.cs
LobbyUIController.cs

(Toplam: Core 10, Network 8, Player 4, Systems 4, UI 4 = **30** dosya — not: özette "29" denmişti,
gerçek sayım 30'dur, `RoundState.cs` dahil.)
### Assets/Prefabs
Assets/Prefabs/Player.prefab (tek prefab, projede başka prefab yok)

### Assets/Settings
Assets/Settings/DefaultVolumeProfile.asset
Assets/Settings/Localization Settings.asset
Assets/Settings/Mobile_Renderer.asset
Assets/Settings/Mobile_RPAsset.asset
Assets/Settings/PC_Renderer.asset
Assets/Settings/PC_RPAsset.asset
Assets/Settings/SampleSceneProfile.asset
Assets/Settings/UniversalRenderPipelineGlobalSettings.asset

### ScriptableObject Asset'leri (Assets/Data)
Assets/Data/Ingredients/Ekmek.asset (IngredientType)
Assets/Data/Ingredients/Kofte.asset (IngredientType)
Assets/Data/Recipes/NormalHamburger.asset (BurgerRecipe)
Assets/Data/Emotes/EmoteA.asset (EmoteDefinition) + EmoteA_Icon.png
Assets/Data/Emotes/EmoteB.asset (EmoteDefinition) + EmoteB_Icon.png
Assets/Data/Emotes/EmoteC.asset (EmoteDefinition) + EmoteC_Icon.png

---
## 2. HER SINIFIN PUBLIC YÜZEYİ
### `Assets/Scripts/Core/TransportMode.cs`
```csharp
public enum TransportMode { Steam, LocalUdp }
Assets/Scripts/Core/PlayerRole.cs
public enum PlayerRole { None, Kasiyer, Yamak, Sef }
Assets/Scripts/Core/RoundState.cs
public enum RoundState { Lobby, RoundActive, RoundEnded }
Assets/Scripts/Core/InteractionType.cs
public enum InteractionType { Press, Hold }
Assets/Scripts/Core/IRoleAssignmentStrategy.cs
public interface IRoleAssignmentStrategy
{
    PlayerRole AssignRole(ulong clientId, int joinOrderIndex);
}
Assets/Scripts/Core/SequentialRoleAssignmentStrategy.cs
public class SequentialRoleAssignmentStrategy : IRoleAssignmentStrategy
{
    public PlayerRole AssignRole(ulong clientId, int joinOrderIndex);
}
private static readonly PlayerRole[] JoinOrder = { Sef, Yamak, Kasiyer }
Assets/Scripts/Core/IVoiceProvider.cs
public interface IVoiceProvider
{
    bool ShouldTransmitLocalVoice { get; }
    void Initialize();
    void Shutdown();
    void Tick();
    void SetLocalCaptureMuted(bool muted);
    bool TryReadLocalVoicePacket(out byte[] packet);
    void ConfigureRemoteSpeaker(GameObject speakerObject, AudioSource speakerSource);
    void DecompressAndEnqueue(AudioSource speakerSource, byte[] compressedPacket);
}
Assets/Scripts/Core/IngredientType.cs — ScriptableObject
[CreateAssetMenu(fileName = "IngredientType", menuName = "Cook No Evil/Ingredient Type")]

[SerializeField] private int id
[SerializeField] private string localizationKey
[SerializeField] private Sprite icon
[SerializeField] private bool isBread
public int Id => id
public string LocalizationKey => localizationKey
public Sprite Icon => icon
public bool IsBread => isBread
Assets/Scripts/Core/BurgerRecipe.cs — ScriptableObject
[CreateAssetMenu(fileName = "BurgerRecipe", menuName = "Cook No Evil/Burger Recipe")]

[Serializable]
public struct IngredientRequirement
{
    public IngredientType Type;
    public int Quantity;
}

public class BurgerRecipe : ScriptableObject
{
    public string RecipeName => recipeName;
    public IReadOnlyList<IngredientRequirement> RequiredIngredients => requiredIngredients;
    public IReadOnlyList<IngredientType> ExcludedIngredients => excludedIngredients;
    public bool AllowsIngredient(IngredientType type);
}
[SerializeField] private string recipeName
[SerializeField] private List<IngredientRequirement> requiredIngredients = new()
[SerializeField] private List<IngredientType> excludedIngredients = new()
Assets/Scripts/Core/EmoteDefinition.cs — ScriptableObject
[CreateAssetMenu(fileName = "EmoteDefinition", menuName = "Cook No Evil/Emote Definition")]

[SerializeField] private string localizationKey
[SerializeField] private Sprite icon
[SerializeField] private Color reactionColor = Color.white
[SerializeField] private string displayName
[SerializeField] [TextArea] private string description
public string LocalizationKey => localizationKey
public Sprite Icon => icon
public Color ReactionColor => reactionColor
public string DisplayName => displayName
public string Description => description
Assets/Scripts/Network/VoiceStreamPlayer.cs
[RequireComponent(typeof(AudioSource))] public class VoiceStreamPlayer : MonoBehaviour

private const int BufferSeconds = 2
private const int CarrierSampleRate = 48000
public AudioSource Source { get; set; }
public void Enqueue(float[] samples)
private void Awake()
private void OnAudioFilterRead(float[] data, int channels)
Assets/Scripts/Network/MockVoiceProvider.cs
public class MockVoiceProvider : IVoiceProvider

public bool ShouldTransmitLocalVoice => false
Tüm IVoiceProvider metotları implement edilmiş.
private static AudioClip CreateDummyToneClip()
Assets/Scripts/Network/SteamworksVoiceProvider.cs
public class SteamworksVoiceProvider : IVoiceProvider

public bool ShouldTransmitLocalVoice => true
private readonly MemoryStream _decompressStream = new(1024 * 16)
private bool _localMuted
Tüm IVoiceProvider metotları implement edilmiş.
private static void SyncDecodeSampleRateToOutput()
Assets/Scripts/Network/NetworkTransportManager.cs
public class NetworkTransportManager : MonoBehaviour

public static NetworkTransportManager Instance { get; private set; }
[SerializeField] private TransportMode defaultMode = TransportMode.Steam
[SerializeField] private FacepunchTransport steamTransport
[SerializeField] private UnityTransport localUdpTransport
public TransportMode CurrentMode { get; private set; }
public void ConfigureTransport(TransportMode mode)
public void SetSteamHostTarget(ulong hostSteamId)
Assets/Scripts/Network/PlayerSpawner.cs
[RequireComponent(typeof(NetworkObject))] public class PlayerSpawner : NetworkBehaviour

[SerializeField] private NetworkObject playerPrefab
[SerializeField] private float spawnSpacing = 2f
private readonly Dictionary<PlayerRole, NetworkObject> _spawnedPlayerObjects = new()
public override void OnNetworkSpawn()
public override void OnNetworkDespawn()
private void HandleServerRoleAssigned(ulong clientId, PlayerRole role)
Assets/Scripts/Network/SteamLobbyManager.cs
public class SteamLobbyManager : MonoBehaviour

public static SteamLobbyManager Instance { get; private set; }
public event Action OnLobbyCreated
public event Action OnLobbyJoined
public event Action<string> OnLobbyError
public event Action OnHostDisconnected
[SerializeField] private NetworkTransportManager transportManager
[SerializeField] private int maxLobbyMembers = 3
private const string ConnectPrefix = "+connect_lobby "
private Lobby? _currentLobby
private bool _networkBusy
public bool IsInLobby => _currentLobby.HasValue
public bool IsHost { get; private set; }
public async void HostLobby()
private static void WriteSteamIdToConnectionData()
private void AdvertiseLobbyPresence(SteamId lobbyId)
public void OpenInviteOverlay()
private void HandleGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
private void HandleRichPresenceJoinRequested(Friend friend, string connectString)
private async void JoinLobby(SteamId lobbyId)
private void HandleLobbyMemberLeave(Lobby lobby, Friend friend)
private void HandleClientDisconnect(ulong clientId)
private void HandleTransportFailure()
private void HandleHostLost()
public void LeaveLobby()
private void OnApplicationQuit()
private IEnumerator WaitForNetworkShutdown(NetworkManager networkManager)
private IEnumerator ClearStaleRichPresenceOnStartup()
Assets/Scripts/Network/RoleManager.cs
[RequireComponent(typeof(NetworkObject))] public class RoleManager : NetworkBehaviour

public static RoleManager Instance { get; private set; }
public const int MaxPlayers = 3
private const string LobbyFullReasonKey = "error.lobby_full"
private const string RoundInProgressReasonKey = "error.round_in_progress"
public event Action<PlayerRole> OnLocalRoleAssigned
public event Action<ulong, PlayerRole> OnServerRoleAssigned
private readonly NetworkList<ClientRoleEntry> _assignedRoles = new()
private IRoleAssignmentStrategy _strategy
private readonly Dictionary<ulong, ulong> _pendingSteamIdByClientId = new()
public int AssignedRoleCount => _assignedRoles.Count
public PlayerRole GetRole(ulong clientId)
public PlayerRole LocalRole => NetworkManager == null ? PlayerRole.None : GetRole(NetworkManager.LocalClientId)
private void Awake(), private void Start()
public override void OnNetworkSpawn(), public override void OnNetworkDespawn()
private void HandleClientDisconnectedOnServer(ulong clientId)
private void OnDestroy()
private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
private static ulong DecodeSteamId(byte[] payload)
private int FindFrozenEntryIndex(ulong steamId)
private void HandleClientConnected(ulong clientId)
private void HandleAssignedRolesChanged(NetworkListEvent<ClientRoleEntry> change)
Not: IsRoundActive ve StartRound() bu sınıftan tamamen KALDIRILMIŞ (Round State konsolidasyonu ile GameLoopManager'a taşındı).

public readonly struct ClientRoleEntry : IEquatable<ClientRoleEntry>, INetworkSerializeByMemcpy

public readonly ulong ClientId
public readonly PlayerRole Role
public readonly ulong SteamId
public readonly bool IsFrozen
public ClientRoleEntry(ulong clientId, PlayerRole role, ulong steamId, bool isFrozen)
public bool Equals(ClientRoleEntry other), public override bool Equals(object obj), public override int GetHashCode()
Assets/Scripts/Network/VoIPController.cs
[RequireComponent(typeof(NetworkObject))] public class VoIPController : NetworkBehaviour

[SerializeField] private float yamakLowPassCutoffHz = 800f
private IVoiceProvider _voiceProvider
private readonly Dictionary<ulong, VoiceStreamPlayer> _speakerPlayers = new()
private PlayerRole _localRole = PlayerRole.None
private static bool IsRoundActive => GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive
public override void OnNetworkSpawn(), public override void OnNetworkDespawn()
private void Update()
private void HandleLocalRoleAssigned(PlayerRole role)
private void HandleRoundStateChanged(RoundState previous, RoundState current)
private void UpdateLocalMuteState()
[ServerRpc(RequireOwnership = false)] private void SendVoiceServerRpc(byte[] compressedData, ServerRpcParams rpcParams = default)
[ClientRpc] private void ReceiveVoiceClientRpc(ulong senderId, byte[] compressedData)
private VoiceStreamPlayer GetOrCreateSpeakerPlayer(ulong speakerId)
private void ApplyRoleBasedAudioSettings(AudioSource source)
Assets/Scripts/Player/PlayerController.cs
[RequireComponent(typeof(CharacterController))] public class PlayerController : NetworkBehaviour

[SerializeField] private InputActionAsset inputActions
[SerializeField] private Transform cameraPivot
[SerializeField] private Camera playerCamera
[SerializeField] private float moveSpeed = 5f
[SerializeField] private float mouseSensitivity = 0.12f
[SerializeField] private float minPitch = -80f
[SerializeField] private float maxPitch = 80f
[SerializeField] private float gravity = -20f
public override void OnNetworkSpawn()
protected override void OnOwnershipChanged(ulong previous, ulong current)
private void ApplyOwnershipState(), private void ApplyNonOwnerState()
private void Update(), private void ApplyLook(), private void ApplyMove()
Not (Section 9'da tekrar geçecek): Prefab'da hâlâ sprintMultiplier diye stale/eski bir serileştirilmiş değer var (1.6), ama bu sınıfta artık böyle bir alan YOK — sprint tamamen kaldırılmış, prefab YAML'inde orphan/kullanılmayan bir değer olarak kalmış.

Assets/Scripts/Player/PlayerInteractor.cs
[RequireComponent(typeof(NetworkObject))] public class PlayerInteractor : NetworkBehaviour

[SerializeField] private InputActionAsset inputActions
[SerializeField] private Camera playerCamera
[SerializeField] private float interactRange = 2.5f
[SerializeField] private LayerMask interactableLayer
public override void OnNetworkSpawn()
protected override void OnOwnershipChanged(ulong previous, ulong current)
private void ApplyOwnershipState(), private void ApplyNonOwnerState(), private void UnsubscribeAttackAction()
public override void OnNetworkDespawn()
private void HandleAttackStarted(InputAction.CallbackContext context)
[ServerRpc] private void ReportInteractionAttemptServerRpc(bool succeeded)
[ClientRpc] private void InteractionAttemptClientRpc(bool succeeded)
private void HandleAttackCanceled(InputAction.CallbackContext context)
private bool TryGetCurrentTarget(out HoldOrPressInteractable interactable)
Assets/Scripts/Player/PlayerInventory.cs
[RequireComponent(typeof(NetworkObject))] public class PlayerInventory : NetworkBehaviour

public const int SlotCount = 4
public const int EmptySlot = -1
public readonly NetworkList<int> Slots = new()
public readonly NetworkVariable<int> ActiveSlotIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner)
public override void OnNetworkSpawn()
public void SetActiveSlot(int slotIndex)
public bool ServerTryAddItem(int ingredientId)
public bool ServerTryRemoveItem(int slotIndex)
public bool ServerTryRemoveActiveItem(out int ingredientId)
Assets/Scripts/Player/PlayerEmoteReactor.cs
public class PlayerEmoteReactor : NetworkBehaviour

[SerializeField] private Transform visualRoot
[SerializeField] private Renderer visualRenderer
[SerializeField] private float flashDuration = 0.6f
[SerializeField] private float bounceHeight = 0.2f
[SerializeField] private float tiltAngle = 12f
private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor")
public override void OnNetworkSpawn(), public override void OnNetworkDespawn()
private void HandleEmoteTriggered(ulong kasiyerClientId, int emoteIndex)
private IEnumerator PlayReaction(Color color)
Not: Bu sınıf [RequireComponent(typeof(NetworkObject))] TAŞIMIYOR (Player.prefab'da zaten var olan NetworkObject'e güveniyor).

Assets/Scripts/Systems/HoldOrPressInteractable.cs
[DisallowMultipleComponent] public class HoldOrPressInteractable : MonoBehaviour

[SerializeField] private InteractionType interactionType = InteractionType.Press
[SerializeField] private float holdDuration = 2.5f
public event Action OnPressBegin
public event Action OnPressEnd
public event Action OnInteractionCompleted
public event Action OnInteractionCancelled
public bool IsPressed { get; private set; }
public void BeginPress()
public void EndPress()
private void Update()
Assets/Scripts/Systems/BurgerAssemblyStation.cs
[RequireComponent(typeof(NetworkObject))] [RequireComponent(typeof(HoldOrPressInteractable))] public class BurgerAssemblyStation : NetworkBehaviour

[SerializeField] private BurgerRecipe activeRecipe
[SerializeField] private IngredientType[] registeredIngredients
public readonly NetworkList<int> PlacedIngredients = new()
private HoldOrPressInteractable _interactable
private void Awake(), private void OnEnable(), private void OnDisable()
private void HandleInteractionCompleted()
[ServerRpc(RequireOwnership = false)] private void PlaceIngredientServerRpc(ServerRpcParams rpcParams = default)
private bool IsRecipeComplete()
private IngredientType FindIngredientType(int id)
Assets/Scripts/Systems/GameLoopManager.cs
[RequireComponent(typeof(NetworkObject))] public class GameLoopManager : NetworkBehaviour

public static GameLoopManager Instance { get; private set; }
public readonly NetworkVariable<RoundState> CurrentRoundState = new(RoundState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
private readonly NetworkVariable<bool> _isPaused = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
public bool IsRoundActive => CurrentRoundState.Value == RoundState.RoundActive
public bool IsGamePaused => IsRoundActive && _isPaused.Value (computed property, NetworkVariable DEĞİL)
private void Awake(), private void OnDestroy()
public override void OnNetworkSpawn()
public bool StartRound()
public void ServerPauseForDisconnect()
public void ServerResumeAfterReconnect()
Assets/Scripts/Systems/EmoteSystem.cs
[RequireComponent(typeof(NetworkObject))] public class EmoteSystem : NetworkBehaviour

public static EmoteSystem Instance { get; private set; }
[SerializeField] private EmoteDefinition[] availableEmotes
[SerializeField] private int yamakEmoteLimit = 1
[SerializeField] private float selectionCooldown = 2.5f
private readonly NetworkVariable<double> _lastSelectionServerTime = new(-1000d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
public event Action<ulong, int> OnEmoteTriggered
public EmoteDefinition[] AvailableEmotes => availableEmotes
public int YamakEmoteLimit => yamakEmoteLimit
public float SelectionCooldown => selectionCooldown
public bool IsOnCooldown => NetworkManager != null && NetworkManager.ServerTime.Time - _lastSelectionServerTime.Value < selectionCooldown
private void Awake(), private void OnDestroy()
[ServerRpc(RequireOwnership = false)] public void SelectEmoteServerRpc(int emoteIndex, ServerRpcParams rpcParams = default)
[ClientRpc] private void EmoteTriggeredClientRpc(ulong kasiyerClientId, int emoteIndex)
Assets/Scripts/UI/HotbarUI.cs
public class HotbarUI : MonoBehaviour

[SerializeField] private InputActionAsset inputActions
[SerializeField] private Image[] slotIcons
[SerializeField] private Image[] slotBackgrounds
[SerializeField] private IngredientType[] registeredIngredients
[SerializeField] private Color normalColor = Color.white
[SerializeField] private Color activeColor = Color.yellow
private PlayerInventory _inventory
private InputAction[] _hotbarActions
private void Start(), private void Update()
private void TryResolveLocalInventory()
private void RefreshVisuals()
private IngredientType FindIngredientType(int id)
Assets/Scripts/UI/InteractionToastUI.cs
public class InteractionToastUI : MonoBehaviour

public static InteractionToastUI Instance { get; private set; }
[SerializeField] private GameObject panel
[SerializeField] private Text messageText
[SerializeField] private float displayDuration = 1.5f
private Coroutine _hideRoutine
private void Awake(), private void OnDestroy()
public void Show(string message)
private IEnumerator HideAfterDelay()
Assets/Scripts/UI/EmoteWheelUI.cs
public class EmoteWheelUI : MonoBehaviour

public static bool IsWheelOpen { get; private set; }
[SerializeField] private InputActionAsset inputActions
[SerializeField] private GameObject wheelRoot
[SerializeField] private Image[] slotIcons
[SerializeField] private Color normalColor = Color.white
[SerializeField] private Color highlightedColor = Color.yellow
[SerializeField] private Image centerIcon
[SerializeField] private Text centerDisplayName
[SerializeField] private Text centerDescription
private InputAction _interactAction
private int _highlightedIndex = -1
private int _activeSlotCount
private bool _wheelOpen
private Vector2 _accumulatedOffset
private void Start(), private void OnDestroy()
private void InitializeWheelIcons()
private static bool IsWheelRole(PlayerRole role)
private void HandleLocalRoleAssigned(PlayerRole role)
private void HandleInteractStarted(InputAction.CallbackContext context)
private void HandleInteractCanceled(InputAction.CallbackContext context)
private void Update()
private void HighlightSlot(int index), private void ResetHighlight()
private void RefreshCenterPanel(int index), private void ClearCenterPanel()
Assets/Scripts/UI/LobbyUIController.cs
public class LobbyUIController : MonoBehaviour

public static LobbyUIController Instance { get; private set; }
private const string TableName = "UIStrings"
[SerializeField] private GameObject lobbyPanel
[SerializeField] private Button hostButton
[SerializeField] private Button inviteButton
[SerializeField] private Button startGameButton
[SerializeField] private Button leaveButton
[SerializeField] private Text statusText
[SerializeField] private GameObject connectionLostPanel
[SerializeField] private Button connectionLostOkButton
[SerializeField] private GameObject gameplayCanvas
public bool ShouldLockCursor { get; private set; }
private void Awake(), private void Start(), private void OnDestroy()
private void HandleHostClicked(), private void HandleInviteClicked(), private void HandleStartGameClicked(), private void HandleLeaveClicked()
private void HandleLobbyCreated(), private void HandleLobbyJoined()
private void HandleLocalRoleAssigned(PlayerRole role)
private void HandleRoundStateChanged(RoundState previous, RoundState current)
private void ApplyRoundActiveState(bool active)
private void SetGameplayCanvasVisible(bool visible)
private void OnApplicationFocus(bool hasFocus)
private void RefreshStatusText()
private void HandleLobbyError(string errorKey)
private void HandleHostDisconnected(), private void HandleConnectionLostOkClicked(), private void ResetToInitialScreen()
private static string LocalizeRole(PlayerRole role)
private static string Localize(string key, params object[] arguments)
3. ENUM'LAR
Enum	Dosya	Değerler (sırayla)
TransportMode	Assets/Scripts/Core/TransportMode.cs	Steam, LocalUdp
PlayerRole	Assets/Scripts/Core/PlayerRole.cs	None, Kasiyer, Yamak, Sef
RoundState	Assets/Scripts/Core/RoundState.cs	Lobby, RoundActive, RoundEnded
InteractionType	Assets/Scripts/Core/InteractionType.cs	Press, Hold
Başka enum yok (IngredientType bir ScriptableObject sınıfıdır, enum DEĞİLDİR — dikkat, isimlendirme benzerliği kafa karıştırabilir).

4. NETWORK KATMANI
NetworkVariable / NetworkList alanları
Sınıf	Alan adı	Generic Tip	Read / Write İzni
GameLoopManager	CurrentRoundState	NetworkVariable<RoundState>	Everyone / Server
GameLoopManager	_isPaused (private)	NetworkVariable<bool>	Everyone / Server
PlayerInventory	Slots	NetworkList<int>	(NetworkList için read/write permission parametresi yok — server-only yazım, kod içi kontrolle)
PlayerInventory	ActiveSlotIndex	NetworkVariable<int>	Everyone / Owner
RoleManager	_assignedRoles (private)	NetworkList<ClientRoleEntry>	(server-only yazım, kod içi kontrolle)
BurgerAssemblyStation	PlacedIngredients	NetworkList<int>	(server-only yazım, kod içi kontrolle)
EmoteSystem	_lastSelectionServerTime (private)	NetworkVariable<double>	Everyone / Server
Not: RoleManager.IsRoundActive ve GameLoopManager.IsGamePaused (eski, NetworkVariable hali) tamamen kaldırıldı — Round State konsolidasyonundan sonra GameLoopManager.CurrentRoundState TEK otorite.

ServerRpc metotları
Metot	Sınıf	Parametreler	RequireOwnership
SendVoiceServerRpc	VoIPController	byte[] compressedData, ServerRpcParams rpcParams = default	false
PlaceIngredientServerRpc	BurgerAssemblyStation	ServerRpcParams rpcParams = default	false
SelectEmoteServerRpc	EmoteSystem	int emoteIndex, ServerRpcParams rpcParams = default	false
ReportInteractionAttemptServerRpc	PlayerInteractor	bool succeeded	varsayılan (true, açıkça belirtilmemiş)
ClientRpc metotları
Metot	Sınıf	Parametreler
ReceiveVoiceClientRpc	VoIPController	ulong senderId, byte[] compressedData
EmoteTriggeredClientRpc	EmoteSystem	ulong kasiyerClientId, int emoteIndex
InteractionAttemptClientRpc	PlayerInteractor	bool succeeded
INetworkSerializeByMemcpy / INetworkSerializable uygulayan struct'lar
Struct	Dosya	Arayüz
ClientRoleEntry	Assets/Scripts/Network/RoleManager.cs (dosyanın sonunda, RoleManager sınıfının dışında tanımlı)	IEquatable<ClientRoleEntry>, INetworkSerializeByMemcpy
BurgerRecipe.IngredientRequirement bir [Serializable] struct'tır ama INetworkSerializeByMemcpy/INetworkSerializable UYGULAMAZ (network üzerinden senkronize edilmiyor, sadece Inspector/ScriptableObject verisi).

5. SAHNE YAPISI (Assets/Scenes/SampleScene.unity)
Kalıcı sahne-içi objeler
GameSystems — bileşenler: Transform, NetworkObject (DontDestroyWithOwner: 0, m_InScenePlaced: 1), VoIPController, RoleManager, EmoteSystem (yamakEmoteLimit: 1, selectionCooldown: 2.5, availableEmotes = EmoteA/B/C), PlayerSpawner (spawnSpacing: 2, playerPrefab = Player.prefab), GameLoopManager.

NetworkBootstrap — AYRI bir obje (GameSystems'ın parçası DEĞİL), bileşenler: Transform, SteamLobbyManager (maxLobbyMembers: 3), NetworkTransportManager (defaultMode: 0 = Steam), UnityTransport (ConnectionData.Address: 127.0.0.1, Port: 7777), FacepunchTransport (steamAppId: 480), NetworkManager (TickRate: 30, EnableSceneManagement: 1, RunInBackground: 1, LogLevel: 1, ConnectionApproval: 0 — kod içinde RoleManager.Start() tarafından runtime'da true'ya çekiliyor).

Canvas'lar
Canvas adı	RenderMode	Bağlı UI script(ler)i	Çocuklar (bilinen)
LobbyCanvas	m_RenderMode: 0 (ScreenSpaceOverlay)	LobbyUIController, GraphicRaycaster, CanvasScaler	hostButton, inviteButton, startGameButton, leaveButton, statusText, connectionLostPanel, connectionLostOkButton, lobbyPanel
GameplayCanvas	m_RenderMode: 0 (ScreenSpaceOverlay)	InteractionToastUI, GraphicRaycaster, CanvasScaler	HotbarPanel (→ HotbarUI'a bağlı, kendi component'i sahnede ayrıca HotbarUI script'ini taşıyan obje 1379181797/HotbarPanel DEĞİL — HotbarUI component'i HotbarPanel üzerinde), EmoteWheelUI'nin root'u (fileID 682034671), InteractionToastUI'nin panel'i (fileID 1771632920)
Not: LobbyUIController.gameplayCanvas alanı sahnede GameplayCanvas objesine (fileID: 334748657) doğrudan bağlı — Inspector referansı, GameObject.Find KULLANILMIYOR (CLAUDE.md'de belgelenen bug fix'i doğrulandı).

Player.prefab — component/hiyerarşi
Player (root)
  Transform (localPosition: 0,1,0)
  CharacterController (height=2, radius=0.5, center=0,0,0)
  NetworkObject (Ownership=1, DontDestroyWithOwner=1, m_InScenePlaced=0)
  NetworkTransform (AuthorityMode=1 [Owner], Interpolate=1)
  PlayerInteractor (interactRange=2.5, interactableLayer.m_Bits=256 [layer 8])
  PlayerInventory
  PlayerController (moveSpeed=5, mouseSensitivity=0.12, minPitch=-80, maxPitch=80, gravity=-20)
  PlayerEmoteReactor (flashDuration=0.6, bounceHeight=0.2, tiltAngle=12)
  ├── CameraPivot (Transform, localPosition: 0, 0.6, 0)
  │     └── Player Camera (Transform, Camera, AudioListener)
  └── Visual (Transform, MeshFilter [Capsule mesh], MeshRenderer)
Tespit edilen, kod dışı bir bulgu: PlayerController component'inin serileştirilmiş verisinde hâlâ sprintMultiplier: 1.6 alanı YAZILI duruyor — ama güncel PlayerController.cs'de böyle bir alan artık YOK (sprint kaldırıldı). Bu, Unity'nin eski/kaldırılmış alanları prefab YAML'inde sessizce bırakmasından kaynaklanan zararsız bir kalıntı (bkz. Bölüm 9).

6. INPUT (Assets/InputSystem_Actions.inputactions)
"Player" action map
Action	Tip	Bağlı tuş/kontrol (Keyboard&Mouse)
Move	Value (Vector2)	WASD composite (W/A/S/D) + Gamepad leftStick
Look	Value (Vector2)	<Pointer>/delta (mouse) + Gamepad rightStick
Attack	Button	<Mouse>/leftButton, ayrıca <Keyboard>/enter
Interact	Button, interactions: "Hold"	<Keyboard>/e
Crouch	Button	<Keyboard>/c
Jump	Button	<Keyboard>/space
Previous	Button	<Keyboard>/1
Next	Button	<Keyboard>/2
HotbarSlot1	Button	<Keyboard>/1
HotbarSlot2	Button	<Keyboard>/2
HotbarSlot3	Button	<Keyboard>/3
HotbarSlot4	Button	<Keyboard>/4
Not: Interact action'ının binding'inde interactions: "Hold" yazıyor ama EmoteWheelUI.cs kod yorumunda "kendi ham started/canceled'ını okuyor, Unity'nin kendi Hold interaction'ına bağımlı değil" deniyor — bu iki bilgi çelişkili görünüyor (action asset'inde Hold interaction tanımlı ama kod bunu görmezden gelip ham started/canceled event'lerini kullanıyor gibi davranıyor). Tespit edilemedi: bu Hold interaction'ın gerçek çalışma zamanı davranışı üzerinde bir etkisi olup olmadığı (örn. started'ın ne zaman tetiklendiği Hold interaction'la değişebilir) bu pasif kod okumasıyla kesin olarak doğrulanamadı.

Previous/Next action'ları 1/2 tuşlarına bağlı — bu, HotbarSlot1/HotbarSlot2 ile ÇAKIŞIYOR (aynı fiziksel tuşlar iki farklı action'a bağlı). Kod tarafında Previous/Next action'larını okuyan hiçbir script bulunamadı (muhtemelen kullanılmıyor, eski/varsayılan template kalıntısı). Tespit edilemedi: bu action'ların gerçekten hiç kullanılmadığı kesin değil, sadece bu pass'te okunan .cs dosyalarında referansı bulunamadı.

Eski "Sprint" action'ı (Shift+koşma) ve 3 binding'i tamamen SİLİNMİŞ — kod ve action asset'inde hiçbir izi yok, doğrulandı.

"UI" action map
Navigate, Submit, Cancel, Point, Click, RightClick, MiddleClick, ScrollWheel, TrackedDevicePosition, TrackedDeviceOrientation — standart Unity input template'i, projeye özel bir değişiklik tespit edilmedi.

Control Scheme'ler
Keyboard&Mouse, Gamepad, Touch, Joystick, XR — hepsi standart template.

7. PAKET VE SÜRÜMLER
Unity sürümü: 6000.5.7f1 (revizyon 017862109af0) — ProjectSettings/ProjectVersion.txt'ten doğrulandı.
NGO (com.unity.netcode.gameobjects): 2.13.1
Facepunch transport köprü paketi:
com.community.netcode.transport.facepunch:
https://github.com/Unity-Technologies/multiplayer-community-contributions.git?path=/Transports/com.community.netcode.transport.facepunch#0eda04fc2146a4f907a61de6403315bce705279e
(pinned commit: 0eda04fc2146a4f907a61de6403315bce705279e)
Diğer paketler (Packages/manifest.json)
Paket	Sürüm
com.coplaydev.unity-mcp	git URL, #main branch (pinned değil)
com.unity.ai.navigation	2.0.14
com.unity.collab-proxy	2.13.5
com.unity.ide.rider	3.0.38
com.unity.ide.visualstudio	2.0.26
com.unity.inputsystem	1.20.0
com.unity.localization	1.5.12
com.unity.multiplayer.center	1.0.1
com.unity.multiplayer.playmode	2.0.2
com.unity.render-pipelines.universal (URP)	17.5.0
com.unity.test-framework	1.7.0
com.unity.timeline	1.8.12
com.unity.ugui	2.5.0
com.unity.visualscripting	1.9.12
(+ standart com.unity.modules.* — accessibility, ai, animation, audio, physics, ui, vb.)

8. ÇALIŞAN / ÇALIŞMAYAN
VERİFİYE EDİLMİŞ, doğrulanmış çalışan
Steam lobi kurma/katılma, host/client bağlantısı — 3 farklı gerçek Steam hesabıyla uçtan uca test edildi (Bileşen 1 raporu).
Rol atama (Sef→Yamak→Kasiyer sırası) + lobi-fazı disconnect/rejoin index düzeltmesi — reflection ile Play Mode'da test edildi (100/101/102 sırayla katılım, 100 ayrılıp 200 katılınca doğru rol).
Round State konsolidasyonu (RoundState enum, GameLoopManager.CurrentRoundState tek otorite, IsGamePaused computed property) — reflection ile 4 senaryo test edildi: Lobby'de pause no-op, 2→3 oyuncu StartRound, round-içi disconnect pause+yabancı SteamId reddi+gerçek SteamId reconnect, RoundEnded'da pause no-op.
"Oyun durduruldu" round-içi disconnect/reconnect SteamId eşleştirmesi — reflection ile tam senaryo test edildi (disconnect→freeze→yabancı red→gerçek reconnect→resume). Ama PlayerSpawner'ın gerçek ChangeOwnership dalı bu reflection testinin kapsamı DIŞINDA kaldı, sadece kod incelemesiyle doğrulandı.
OnOwnershipChanged guard'ı (host'un kopan oyuncuyu "kendisininmiş gibi" görmesi bug'ının düzeltmesi) — Play Mode'da OnOwnershipChanged(999, ServerClientId) reflection ile simüle edilip enabled=False olduğu doğrulandı.
Emote çarkı rol-bazlı erişim (Kasiyer tam liste, Yamak yamakEmoteLimit ile kısıtlı, Sef hiç erişemez), cooldown, açı→index hesaplaması — reflection ile doğrulandı.
Round başlayınca lobbyPanel→GameplayCanvas geçişi, imleç kilidi — host'un gerçek Player.log'unda teşhis satırlarıyla doğrulandı (3 gerçek Steam makine testi).
Steam overlay/"Arkadaş Davet Et" çalışmama sorunu — kök nedeni KESİNLEŞTİ: SteamUtils.IsOverlayEnabled = False, environment kısıtı (development build resmi Steamworks depot'una kayıtlı değil), KOD HATASI DEĞİL. Kullanıcı bu konuda başka kod değişikliği istemiyor.
YAZILMIŞ AMA TEST EDİLMEMİŞ (gerçek çok-makineli/2. bağlı client testinde doğrulanmamış)
PlayerController/PlayerInteractor'daki OnOwnershipChanged'in GERÇEK bir 2. bağlı client'a sahiplik devrinde doğru çalıştığı — sadece host'un kendi objesiyle simüle edilebildi (tek process ortamı), gerçek "sahiplik BAŞKA bir client'a geçince doğru donuyor mu" yönü test edilemedi.
BurgerAssemblyStation.PlaceIngredientServerRpc — kod incelemesiyle mantıksal olarak doğru görünüyor ama hiçbir Play Mode/gerçek test raporunda bu RPC'nin çağrıldığına/malzeme yerleştirmenin çalıştığına dair bir doğrulama bulunamadı.
LMB etkileşiminin (PlayerInteractor) gerçek build'de neden başarısız olduğu KESİN olarak çözülmedi — en olası hipotez (düşük hedef + görsel geri bildirim yokluğu + dar menzil) kanıtlanamadı, teşhis logu bir sonraki gerçek teste bırakıldı.
HotbarUI'nin round-içi reconnect sonrası doğru envanteri gösterdiği — kod incelemesiyle (NGO kaynak koduna referansla) yüksek güvenle doğru kabul ediliyor ama gerçek bir reconnect testinde ekran üzerinden doğrulanmadı.
BİLİNEN AÇIK BUGLAR
Round başlama sırasında "Rolün:" yazısının bazen boş kaldığı raporu — gerçek 2-process local-UDP testinde (kasıtlı race dahil) tekrar üretilemedi, teşhis logu ([LobbyUIController] Round baslama teshis:) bırakıldı, henüz kaldırılmadı.
Previous/Next input action'larının HotbarSlot1/HotbarSlot2 ile aynı tuşları (1/2) paylaşması — bu envanter pass'inde YENİ tespit edildi, kod tarafında bir çakışma etkisi olup olmadığı doğrulanmadı (bkz. Bölüm 6).
BİLİNÇLİ OLARAK EKSİK BIRAKILAN ŞEYLER
"Hedefe bakınca vurgula" prompt/highlight UI'ı YOK — HoldOrPressInteractable tamamen headless, hiçbir görsel geri bildirim yok.
Gerçek seviye geometrisi YOK — sadece TestLevel altında Floor (Plane) + 4 duvar (Cube'lar) gri-kutu test seviyesi.
Müşteri/Sipariş/NPC/kuyruk sistemi hiç KURULMADI — BurgerAssemblyStation sadece tarif doğrulama mantığı, gerçek bir sipariş kaynağına bağlı değil, activeRecipe Inspector'dan sabit.
Yamak'ın kısıtlı emote listesinin İÇERİĞİ tasarlanmadı — sadece SAYI (yamakEmoteLimit) kısıtlandı.
RoundEnded'a geçiş mantığı hiç kurulmadı — sadece enum değeri tanımlı, hiçbir kod bu state'e geçmiyor.
"Oyun durduruldu" görsel/işitsel göstergesi (ekranda "X bekleniyor" yazısı, freeze VFX) YOK.
Bileşen 3 (CookingStateMachine, FireEventSystem) ve Bileşen 2'nin geri kalanı (IntercomSystem, DumbwaiterSystem, 5 dk sayaç, 3 strike, skor hedefi) hiç KURULMADI.
9. GEÇİCİ / TEST AMAÇLI KURULUMLAR
steam_appid.txt içeriği 480 (Spacewar, Valve'ın test için verdiği ortak AppID) — NetworkBootstrap objesindeki FacepunchTransport.steamAppId alanında da 480 olarak sahnede sabit. Gerçek bir Steam AppID alınınca hem bu dosya silinmeli hem steamAppId alanı güncellenmeli.
Local UDP transport port'u: 7777, adres 127.0.0.1 — NetworkBootstrap üzerindeki UnityTransport.ConnectionData içinde sabit.
TestLevel gri-kutu test seviyesi — Floor (Plane) + Wall_North/South/East/West (Cube'lar), final tasarım DEĞİL, sadece hareket/etkileşim test edilebilsin diye.
BurgerAssemblyStation.activeRecipe Inspector'dan sabit NormalHamburger.asset'e bağlı — gerçek bir sipariş/müşteri sisteminden gelmiyor, test amaçlı sabit seçim.
NormalHamburger.asset tarifi: requiredIngredients = [Ekmek × 2, Kofte × 1] — bu değerlerin final tasarım mı yoksa placeholder mı olduğu tespit edilemedi (GDD'de tarif içerikleri tanımlı değil).
Ekmek.asset (id=0, isBread=1), Kofte.asset (id=1, isBread=0) — icon alanları HER İKİSİNDE de {fileID: 0} (boş/atanmamış) — yani hotbar'da bu malzemelerin ikonu şu an GÖRÜNMEYECEK (sprite yok).
RoleManager/GameLoopManager/EmoteSystem/PlayerSpawner/VoIPController GameSystems objesi üzerinde, SteamLobbyManager/NetworkTransportManager/NetworkManager/transport'lar ayrı bir NetworkBootstrap objesi üzerinde — bu ayrım muhtemelen kasıtlı (NetworkObject taşıyan vs taşımayan bileşenlerin ayrılması) ama CLAUDE.md'de bu iki-obje mimarisi açıkça belgelenmemiş, sadece "GameSystems" tekil isim olarak geçiyor — tespit edilemedi: bu ayrımın bilinçli bir mimari karar mı yoksa isimlendirme/organizasyon geçmişinin bir kalıntısı mı olduğu.
Player.prefab'ın PlayerController component'inde stale sprintMultiplier: 1.6 serileştirilmiş alanı — kodda artık böyle bir alan yok (sprint kaldırıldı), Unity bu değeri sessizce prefab YAML'inde bırakmış. Zararsız ama temizlenmesi gereken bir kalıntı.
PlayerInteractor.HandleAttackStarted içindeki teşhis Debug.Log (camPos/camForward basan) — LMB etkileşim sorununu teşhis etmek için eklendi, kalıcı bir loglama değil, sorun netleşince kaldırılmalı.
LobbyUIController.HandleRoundStateChanged içindeki teşhis Debug.Log (Round baslama teshis: LocalRole=... statusText=...) — "Rolün:" boş kalma bugu için eklendi, henüz kaldırılmadı.
InteractionToastUI tamamen geçici/test amaçlı bir araç olarak belgelenmiş (CLAUDE.md'de açıkça "kalıcı bir oyun mekaniği DEĞİL" yazıyor) — LMB etkileşim denemelerini ekranda göstermek için.
Previous/Next input action'ları HotbarSlot1/HotbarSlot2 ile aynı tuşları (1/2) paylaşıyor — muhtemelen eski/kullanılmayan bir template kalıntısı, temizlenmesi gerekebilir (bkz. Bölüm 8).
Rapor tek bir markdown bloğu olarak yukarıda; kod veya proje üzerinde hiçbir değişiklik yapılmadı, sadece okuma yapıldı.