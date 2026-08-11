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

    private void TryResolveLocalInventory()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsConnectedClient)
            return;

        var playerObject = networkManager.LocalClient?.PlayerObject;
        if (playerObject == null)
            return;

        _inventory = playerObject.GetComponent<PlayerInventory>();
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
