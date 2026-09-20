using UnityEngine;
using UnityEngine.UI;

// GDD 4.1.2 (1): ekran ortasindaki crosshair, bakilan hedefe gore uc durum gosterir
// (Notr / Kullanilabilir / Engelli). Durum her karede yerel olarak PlayerInteractor.Local'dan
// OKUNUR — RPC yoktur. "Engelli" sebep gostermez; uc durumun gorseli (sprite/renk/boyut)
// Inspector'dan ayarlanir. Ekran ustu UI oldugu icin Sef'in kontur render'indan etkilenmez.
public class CrosshairUI : MonoBehaviour
{
    [System.Serializable]
    public class StateVisual
    {
        public Sprite sprite;
        public Color color = Color.white;
        public Vector2 size;
    }

    [SerializeField] private Image image;
    [SerializeField] private StateVisual neutral;
    [SerializeField] private StateVisual usable;
    [SerializeField] private StateVisual blocked;

    private CrosshairState? _applied;

    private void OnEnable()
    {
        _applied = null;
    }

    private void Update()
    {
        var interactor = PlayerInteractor.Local;
        var state = interactor != null ? interactor.Feedback : CrosshairState.Neutral;

        if (_applied == state)
            return;

        Apply(state);
        _applied = state;
    }

    private void Apply(CrosshairState state)
    {
        var visual = state switch
        {
            CrosshairState.Usable => usable,
            CrosshairState.Blocked => blocked,
            _ => neutral,
        };

        image.sprite = visual.sprite;
        image.color = visual.color;
        image.rectTransform.sizeDelta = visual.size;
    }
}
