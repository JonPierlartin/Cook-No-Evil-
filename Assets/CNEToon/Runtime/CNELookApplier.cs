using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// CNE Toon görünümünü OYUNDA, çalışırken uygular (deneme: eski görünümle yan yana karşılaştırılabilsin diye materyal
// dosyaları ve prefab'lar kalıcı olarak çevrilmedi). Kendini kurar; sahnede nesnesi yoktur.
//
// Oyuncu ayarlardan bu görünümü seçmişse, yerel rolü izinli rollerdense ve round sürüyorsa:
//  - opak kaynak-shader (URP Lit) materyalleri CNE/Toon kopyalarıyla değiştirir (doku ve renk asıldan, diğer
//    ayarlar şablon materyalden),
//  - karakterlere, öğelere ve etkileşilen nesnelere Outline rendering layer'ını ekler (diğer bitler korunur),
//  - ışığı ayarlar (güneş rengi / şiddeti, düz koyu ortam ışığı),
//  - CNELook.Active'i açar (post-process ve outline ona bağlıdır).
// Koşul bozulunca hepsini geri alır. Oynanışı etkilemez; ağdan hiçbir şey gitmez.
public class CNELookApplier : MonoBehaviour
{
    private const string LookId = "cne";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private struct Lighting
    {
        public AmbientMode AmbientMode;
        public Color AmbientLight;
        public Light Sun;
        public Color SunColor;
        public float SunIntensity;
    }

    private readonly Dictionary<Material, Material> _surfaceByOriginal = new();
    private readonly Dictionary<Material, Material> _characterByOriginal = new();
    private readonly Dictionary<Renderer, Material[]> _originalsByRenderer = new();
    // Outline bitini BİZİM eklediğimiz renderer'lar (zaten taşıyanlara geri alırken dokunulmaz).
    private readonly HashSet<Renderer> _outlined = new();
    private readonly List<Renderer> _dead = new();

    private CNELookSettings _settings;
    private Shader _sourceShader;
    private Lighting _savedLighting;
    private bool _applied;
    private float _nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var settings = CNELookSettings.Load();
        if (settings == null || settings.surfaceTemplate == null)
            return;

        LookPreference.Register(LookId, settings.optionLabel);

        var host = new GameObject(nameof(CNELookApplier)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(host);
        host.AddComponent<CNELookApplier>()._settings = settings;
    }

    private void Start()
    {
        _sourceShader = Shader.Find(_settings.sourceShaderName);
    }

    private void OnDestroy()
    {
        if (_applied)
            Restore();
    }

    private void Update()
    {
        bool shouldApply = LookPreference.IsSelected(LookId) && IsLocalRoleInRound();
        if (shouldApply != _applied)
        {
            if (shouldApply)
                Apply();
            else
                Restore();
        }

        if (_applied && Time.unscaledTime >= _nextScan)
        {
            _nextScan = Time.unscaledTime + _settings.scanInterval;
            ConvertRenderers();
        }
    }

    // Yerel oyuncu izinli rollerden biri mi ve oyunda mı (lobide 3B görünüm yok).
    private bool IsLocalRoleInRound()
    {
        if (RoleManager.Instance == null || GameLoopManager.Instance == null)
            return false;

        var network = Unity.Netcode.NetworkManager.Singleton;
        if (network == null || !network.IsListening || GameLoopManager.Instance.CurrentRoundState.Value == RoundState.Lobby)
            return false;

        return System.Array.IndexOf(_settings.roles, RoleManager.Instance.LocalRole) >= 0;
    }

    private void Apply()
    {
        _applied = true;
        _nextScan = 0f;
        CNELook.Active = true;

        if (!_settings.overrideLighting)
            return;

        _savedLighting = new Lighting
        {
            AmbientMode = RenderSettings.ambientMode,
            AmbientLight = RenderSettings.ambientLight,
            Sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight(),
        };

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = _settings.ambientColor;

        if (_savedLighting.Sun != null)
        {
            _savedLighting.SunColor = _savedLighting.Sun.color;
            _savedLighting.SunIntensity = _savedLighting.Sun.intensity;
            _savedLighting.Sun.color = _settings.sunColor;
            _savedLighting.Sun.intensity = _settings.sunIntensity;
        }
    }

    private void Restore()
    {
        _applied = false;
        CNELook.Active = false;

        foreach (var pair in _originalsByRenderer)
        {
            if (pair.Key != null)
                pair.Key.sharedMaterials = pair.Value;
        }

        _originalsByRenderer.Clear();

        uint outlineBit = _settings.outlineLayer.value;
        foreach (var renderer in _outlined)
        {
            if (renderer != null)
                renderer.renderingLayerMask &= ~outlineBit;
        }

        _outlined.Clear();

        if (!_settings.overrideLighting)
            return;

        RenderSettings.ambientMode = _savedLighting.AmbientMode;
        RenderSettings.ambientLight = _savedLighting.AmbientLight;
        if (_savedLighting.Sun != null)
        {
            _savedLighting.Sun.color = _savedLighting.SunColor;
            _savedLighting.Sun.intensity = _savedLighting.SunIntensity;
        }
    }

    private static Light FindDirectionalLight()
    {
        foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type == LightType.Directional)
                return light;
        }

        return null;
    }

    // Sahnedeki (ve sonradan doğan) renderer'ları tarar.
    private void ConvertRenderers()
    {
        _dead.Clear();
        foreach (var renderer in _originalsByRenderer.Keys)
        {
            if (renderer == null)
                _dead.Add(renderer);
        }

        foreach (var renderer in _dead)
            _originalsByRenderer.Remove(renderer);

        _outlined.RemoveWhere(renderer => renderer == null);

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                continue;

            bool character = IsCharacter(renderer);
            ConvertMaterials(renderer, character);
            ApplyOutlineLayer(renderer, character);
        }
    }

    private void ConvertMaterials(Renderer renderer, bool character)
    {
        var materials = renderer.sharedMaterials;
        Material[] converted = null;
        for (int i = 0; i < materials.Length; i++)
        {
            var toon = GetToonMaterial(materials[i], character);
            if (toon == null)
                continue;

            converted ??= (Material[])materials.Clone();
            converted[i] = toon;
        }

        if (converted == null)
            return;

        // İlk çevirişte asıl liste saklanır; sonradan değişen tek bir materyal için asıl liste güncellenir.
        if (!_originalsByRenderer.TryGetValue(renderer, out var originals) || originals.Length != materials.Length)
        {
            _originalsByRenderer[renderer] = materials;
        }
        else
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (converted[i] != materials[i])
                    originals[i] = materials[i];
            }
        }

        renderer.sharedMaterials = converted;
    }

    // Çizgi yalnızca karakterlere ve oynanış nesnelerine: mimari (duvar, zemin, masa) katmana girmez.
    // Elde tutulan öğenin görseli oyuncu karakterinin altında durduğu için karakterle birlikte çizgi alır.
    private void ApplyOutlineLayer(Renderer renderer, bool character)
    {
        uint outlineBit = _settings.outlineLayer.value;
        if (outlineBit == 0 || (renderer.renderingLayerMask & outlineBit) != 0)
            return;

        bool wanted = (character && _settings.outlineCharacters)
            || (_settings.outlineItems && renderer.GetComponentInParent<Item>() != null)
            || (_settings.outlineInteractables && renderer.GetComponentInParent<HoldOrPressInteractable>() != null);
        if (!wanted)
            return;

        renderer.renderingLayerMask |= outlineBit;
        _outlined.Add(renderer);
    }

    private static bool IsCharacter(Renderer renderer)
    {
        return renderer.GetComponentInParent<PlayerCharacterVisual>() != null
            || renderer.GetComponentInParent<Customer>() != null;
    }

    // Opak kaynak-shader materyali için (bir kez üretilen) CNE/Toon kopyası; çevrilmeyecekse null.
    private Material GetToonMaterial(Material original, bool character)
    {
        if (original == null || original.shader != _sourceShader || original.renderQueue >= (int)RenderQueue.AlphaTest)
            return null;

        var cache = character ? _characterByOriginal : _surfaceByOriginal;
        if (cache.TryGetValue(original, out var toon) && toon != null)
            return toon;

        var template = character && _settings.characterTemplate != null ? _settings.characterTemplate : _settings.surfaceTemplate;
        toon = new Material(template) { name = original.name + " (CNE)", hideFlags = HideFlags.DontSave };
        if (original.HasProperty(BaseMapId))
        {
            toon.SetTexture(BaseMapId, original.GetTexture(BaseMapId));
            toon.SetTextureScale(BaseMapId, original.GetTextureScale(BaseMapId));
            toon.SetTextureOffset(BaseMapId, original.GetTextureOffset(BaseMapId));
        }

        if (original.HasProperty(BaseColorId))
            toon.SetColor(BaseColorId, original.GetColor(BaseColorId));

        toon.enableInstancing = original.enableInstancing;
        cache[original] = toon;
        return toon;
    }
}
