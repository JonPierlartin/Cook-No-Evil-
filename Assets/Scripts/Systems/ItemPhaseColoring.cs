using UnityEngine;

// Bir Renderer kumesini bir ItemType.PhaseColors'tan faz indeksine gore boyayan KUCUK paylasilan
// yardimci (MaterialPropertyBlock ile — paylasilan materyali DEGISTIRMEZ). Hem gercek ogenin
// dunya gorseli (ItemPhaseVisual) hem elde tutulan AYRI KOPYA (HeldItemVisual) AYNI kurali
// kullanir — iki farkli renklendirme formulu yazilmaz (K2d, GDD 5.2.1).
public static class ItemPhaseColoring
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static MaterialPropertyBlock _block;

    public static void Apply(Renderer[] renderers, ItemType type, int phaseIndex)
    {
        if (renderers == null || type == null)
            return;

        var colors = type.PhaseColors;
        if (colors == null || phaseIndex < 0 || phaseIndex >= colors.Length)
            return;

        _block ??= new MaterialPropertyBlock();

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, colors[phaseIndex]);
            renderer.SetPropertyBlock(_block);
        }
    }
}
