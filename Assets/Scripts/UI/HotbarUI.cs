using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Her zaman gorunur 4-slotluk hotbar HUD'u. Local player'in PlayerInventory'si
// ancak PlayerSpawner tarafindan spawn edildikten SONRA var oldugu icin (sahne
// baslangicinda henuz yok), Update()'te lazy-resolve edilir — bulununca bir
// dahaki karede referans elde tutulur, tekrar aranmaz.
public class HotbarUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Image[] slotBackgrounds;
    [SerializeField] private IngredientType[] registeredIngredients;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.yellow;

    private PlayerInventory _inventory;
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
            return;
        }
    }

    private void RefreshVisuals()
    {
        int activeIndex = _inventory.ActiveSlotIndex.Value;

        for (int i = 0; i < slotIcons.Length && i < _inventory.Slots.Count; i++)
        {
            int ingredientId = _inventory.Slots[i];
            var ingredientType = FindIngredientType(ingredientId);

            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = ingredientType != null;
                if (ingredientType != null)
                    slotIcons[i].sprite = ingredientType.Icon;
            }

            if (slotBackgrounds != null && i < slotBackgrounds.Length && slotBackgrounds[i] != null)
                slotBackgrounds[i].color = i == activeIndex ? activeColor : normalColor;
        }
    }

    private IngredientType FindIngredientType(int id)
    {
        if (id == PlayerInventory.EmptySlot || registeredIngredients == null)
            return null;

        foreach (var ingredient in registeredIngredients)
        {
            if (ingredient != null && ingredient.Id == id)
                return ingredient;
        }

        return null;
    }
}
