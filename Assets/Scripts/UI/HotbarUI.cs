using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Her zaman gorunur 4-slotluk hotbar HUD'u. Local player'in PlayerInventory'si
// ancak PlayerSpawner tarafindan spawn edildikten SONRA var oldugu icin (sahne
// baslangicinda henuz yok), Update()'te lazy-resolve edilir — bulununca bir
// dahaki karede referans elde tutulur, tekrar aranmaz. Slot icerigi her karede yerel olarak, replike
// slot listesi + spawn edilmis ogelerden okunur (PlayerInventory.TryGetItem): oge listeden gec gelirse
// ikon oge spawn olur olmaz kendiliginden belirir; ag uzerinden hicbir sey gonderilmez.
public class HotbarUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Image[] slotBackgrounds;
    [Tooltip("Slot zemini: secili olmayan / secili. Koyu ve yari saydam tutulur ki her renkten ikon okunabilsin.")]
    [SerializeField] private Color normalColor = new(0.08f, 0.09f, 0.11f, 0.65f);
    [SerializeField] private Color activeColor = new(0.16f, 0.18f, 0.22f, 0.9f);
    [Tooltip("Secili slotun cercevesi (slot basina bir tane; yalnizca secili olan gorunur).")]
    [SerializeField] private Image[] slotFrames;
    [Tooltip("Secili slotun olcegi (digerleri 1).")]
    [SerializeField, Min(1f)] private float activeScale = 1.12f;
    [Tooltip("Kör görüşte (Şef) ikonların çizildiği materyal: yalnızca siluet konturu. Şef dünyada nesneyi kontur " +
        "olarak görür; hotbar renkli ikon gösterirse elindekinin ne olduğunu oradan okur (GDD 4.1.1).")]
    [SerializeField] private Material blindIconMaterial;

    private PlayerInventory _inventory;
    private BlindVisionCamera _blindVision;
    private InputAction[] _hotbarActions;

    private void Start()
    {
        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();

        _hotbarActions = new[]
        {
            playerMap.FindAction("HotbarSlot1"),
            playerMap.FindAction("HotbarSlot2"),
            playerMap.FindAction("HotbarSlot3"),
            playerMap.FindAction("HotbarSlot4"),
        };

        for (int i = 0; i < _hotbarActions.Length; i++)
        {
            int slotIndex = i;
            _hotbarActions[i].performed += _ => _inventory?.SetActiveSlot(slotIndex);
        }
    }

    private void Update()
    {
        if (_inventory == null)
        {
            TryResolveLocalInventory();
            return;
        }

        RefreshVisuals();
    }

    // BULUNAN HATA: NetworkManager.LocalClient.PlayerObject, NGO tarafindan SADECE bir
    // objenin ILK spawn mesaji islenirken guncelleniyor (SpawnNetworkObjectLocallyCommon
    // -> UpdateNetworkClientPlayer) — ChangeOwnership (round-ici reconnect'te
    // PlayerSpawner'in kullandigi yontem) bu alani HIC GUNCELLEMIYOR (NGO paket kaynagi
    // dogrulandi). Bu yuzden reconnect eden oyuncunun kendi client'inda bu alan eski/
    // hic set edilmemis kalabiliyordu. Bunun yerine sahnedeki PlayerController'lar
    // arasinda GERCEKTEN aktif (enabled=true) olani araniyor — PlayerController zaten
    // NGO'nun DontDestroyWithOwner objelerini disconnect'te otomatik olarak
    // ServerClientId'ye devretmesine karsi korumali (bkz. PlayerController'daki
    // OnOwnershipChanged notu), yani bu sinyal IsOwner'in kendisinden daha guvenilir.
    private void TryResolveLocalInventory()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsConnectedClient)
            return;

        foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (!controller.enabled)
                continue;

            _inventory = controller.GetComponent<PlayerInventory>();
            _blindVision = controller.GetComponentInChildren<BlindVisionCamera>(true);
            return;
        }
    }

    private void RefreshVisuals()
    {
        int activeIndex = _inventory.ActiveSlotIndex.Value;
        // null = varsayılan arayüz materyali (renkli ikon).
        var iconMaterial = _blindVision != null && _blindVision.IsBlind ? blindIconMaterial : null;

        for (int i = 0; i < slotIcons.Length && i < _inventory.Slots.Count; i++)
        {
            var itemType = _inventory.TryGetItem(i, out var item) ? item.Type : null;

            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = itemType != null;
                slotIcons[i].material = iconMaterial;
                if (itemType != null)
                    slotIcons[i].sprite = itemType.Icon;
            }

            bool active = i == activeIndex;
            if (slotBackgrounds != null && i < slotBackgrounds.Length && slotBackgrounds[i] != null)
            {
                slotBackgrounds[i].color = active ? activeColor : normalColor;
                slotBackgrounds[i].rectTransform.localScale = Vector3.one * (active ? activeScale : 1f);
            }

            if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                slotFrames[i].enabled = active;
        }
    }
}
