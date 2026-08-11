using UnityEngine;

// Kasiyer'in emote carkindan Yamak'a gonderdigi iletisim token'lari. Belirli bir
// tarif/siparise bagli DEGIL (Order/Recipe sistemi kapsam disi birakildi) — sadece
// soyut bir isaret/ikon seti.
[CreateAssetMenu(fileName = "EmoteDefinition", menuName = "Cook No Evil/Emote Definition")]
public class EmoteDefinition : ScriptableObject
{
    [SerializeField] private string localizationKey;
    [SerializeField] private Sprite icon;

    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
}
