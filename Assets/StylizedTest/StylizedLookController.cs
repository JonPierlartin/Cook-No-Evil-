using System.Collections.Generic;
using UnityEngine;

// DENEME — stilize (toon) görünümü çalışma zamanında uygular. Kendini kurar (sahneye nesne eklemek gerekmez):
// yerel oyuncunun rolü izinli rollerdense (Komi, Kasiyer) ve ayar açıksa, sahnedeki opak URP Lit materyallerini toon
// kopyalarıyla DEĞİŞTİRİR ve ortamı (sis, gradient ortam ışığı, ışık tonu) ayarlar; kapanınca hepsini geri alır.
// Kontur çizgileri (ayar açıksa) her kare kameranın renderer'ına eklenen bir geçişle çizilir (StylizedOutlinePass);
// renderer asset'ine feature eklenmez.
// Materyal dosyalarına dokunulmaz — değişen yalnızca renderer'ların o anki materyal listesidir.
// Açık olup olmadığı oyuncunun yerel görünüm tercihinden okunur (LookPreference; ayarlar kartındaki GÖRÜNÜM satırı).
// Kaldırmak için: Assets/StylizedTest klasörünü sil.
public class StylizedLookController : MonoBehaviour
{
    private const string LegacyPrefKey = "stylizedTest.enabled";
    private const string SettingsResource = "StylizedLookSettings";
    private const string LookId = "stylized";
    private const string LookLabel = "STİLİZE";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int RampId = Shader.PropertyToID("_RampTex");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int AmbientStrengthId = Shader.PropertyToID("_AmbientStrength");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
    private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");
    private static readonly int LineColorId = Shader.PropertyToID("_LineColor");
    private static readonly int LineThicknessId = Shader.PropertyToID("_LineThickness");
    private static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
    private static readonly int NormalThresholdId = Shader.PropertyToID("_NormalThreshold");
    private static readonly int ThinDistanceId = Shader.PropertyToID("_ThinDistance");
    private static readonly int OutlineMaskId = Shader.PropertyToID("_OutlineMask");

    private struct Environment
    {
        public bool Fog;
        public FogMode FogMode;
        public Color FogColor;
        public float FogStart, FogEnd;
        public UnityEngine.Rendering.AmbientMode AmbientMode;
        public Color Sky, Equator, Ground;
        public Light Sun;
        public Color SunColor;
    }

    private StylizedLookSettings _settings;
    private Shader _sourceShader;
    private readonly Dictionary<Material, Material> _toonByOriginal = new();
    // Karakter / müşteri yüzeyleri için ayrı kopyalar (kontur maskesi açık).
    private readonly Dictionary<Material, Material> _maskedToonByOriginal = new();
    private readonly Dictionary<Renderer, Material[]> _originalsByRenderer = new();
    private readonly List<Renderer> _deadRenderers = new();
    private Environment _savedEnvironment;
    private bool _applied;
    private float _nextScan;
    private Material _outlineMaterial;
    private StylizedOutlinePass _outlinePass;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var settings = Resources.Load<StylizedLookSettings>(SettingsResource);
        if (settings == null || settings.toonShader == null)
            return;

        LookPreference.Register(LookId, LookLabel);
        // Oyuncu henüz seçim yapmadıysa eski kutucuğun durumu taşınır (varsayılan açıktı).
        if (!LookPreference.HasSavedSelection)
            LookPreference.Selected = PlayerPrefs.GetInt(LegacyPrefKey, 1) == 1 ? LookId : LookPreference.Off;

        var host = new GameObject("StylizedLook (deneme)");
        DontDestroyOnLoad(host);
        host.AddComponent<StylizedLookController>()._settings = settings;
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
        bool shouldApply = LookPreference.IsSelected(LookId) && IsLocalRoleStylized();
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
    private bool IsLocalRoleStylized()
    {
        if (RoleManager.Instance == null || GameLoopManager.Instance == null)
            return false;

        var network = Unity.Netcode.NetworkManager.Singleton;
        if (network == null || !network.IsListening || GameLoopManager.Instance.CurrentRoundState.Value == RoundState.Lobby)
            return false;

        return System.Array.IndexOf(_settings.roles, RoleManager.Instance.LocalRole) >= 0;
    }

    // Görünüm açıkken her oyun kamerasının renderer'ına kontur geçişi eklenir (kuyruk her kare boşalır).
    private void HandleBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
    {
        if (_outlinePass == null || camera.cameraType != CameraType.Game)
            return;

        var data = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (data != null && data.scriptableRenderer != null)
            data.scriptableRenderer.EnqueuePass(_outlinePass);
    }

    private void StartOutline()
    {
        if (!_settings.outlineEnabled || _settings.outlineShader == null)
            return;

        _outlineMaterial = new Material(_settings.outlineShader) { hideFlags = HideFlags.DontSave };
        _outlineMaterial.SetColor(LineColorId, _settings.outlineColor);
        _outlineMaterial.SetFloat(LineThicknessId, _settings.outlineThickness);
        _outlineMaterial.SetFloat(DepthThresholdId, _settings.outlineDepthThreshold);
        _outlineMaterial.SetFloat(NormalThresholdId, _settings.outlineNormalThreshold);
        _outlineMaterial.SetFloat(ThinDistanceId, _settings.outlineThinDistance);
        _outlinePass = new StylizedOutlinePass(_outlineMaterial);
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
    }

    private void StopOutline()
    {
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        _outlinePass = null;
        if (_outlineMaterial != null)
            Destroy(_outlineMaterial);

        _outlineMaterial = null;
    }

    private void Apply()
    {
        _applied = true;
        _nextScan = 0f;
        StartOutline();

        if (!_settings.overrideEnvironment)
            return;

        _savedEnvironment = new Environment
        {
            Fog = RenderSettings.fog,
            FogMode = RenderSettings.fogMode,
            FogColor = RenderSettings.fogColor,
            FogStart = RenderSettings.fogStartDistance,
            FogEnd = RenderSettings.fogEndDistance,
            AmbientMode = RenderSettings.ambientMode,
            Sky = RenderSettings.ambientSkyColor,
            Equator = RenderSettings.ambientEquatorColor,
            Ground = RenderSettings.ambientGroundColor,
            Sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight()
        };

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = _settings.fogColor;
        RenderSettings.fogStartDistance = _settings.fogStart;
        RenderSettings.fogEndDistance = _settings.fogEnd;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = _settings.skyColor;
        RenderSettings.ambientEquatorColor = _settings.equatorColor;
        RenderSettings.ambientGroundColor = _settings.groundColor;

        if (_savedEnvironment.Sun != null)
        {
            _savedEnvironment.SunColor = _savedEnvironment.Sun.color;
            _savedEnvironment.Sun.color = _savedEnvironment.SunColor * _settings.sunTint;
        }
    }

    private void Restore()
    {
        _applied = false;
        StopOutline();

        foreach (var pair in _originalsByRenderer)
        {
            if (pair.Key != null)
                pair.Key.sharedMaterials = pair.Value;
        }

        _originalsByRenderer.Clear();

        if (!_settings.overrideEnvironment)
            return;

        RenderSettings.fog = _savedEnvironment.Fog;
        RenderSettings.fogMode = _savedEnvironment.FogMode;
        RenderSettings.fogColor = _savedEnvironment.FogColor;
        RenderSettings.fogStartDistance = _savedEnvironment.FogStart;
        RenderSettings.fogEndDistance = _savedEnvironment.FogEnd;
        RenderSettings.ambientMode = _savedEnvironment.AmbientMode;
        RenderSettings.ambientSkyColor = _savedEnvironment.Sky;
        RenderSettings.ambientEquatorColor = _savedEnvironment.Equator;
        RenderSettings.ambientGroundColor = _savedEnvironment.Ground;
        if (_savedEnvironment.Sun != null)
            _savedEnvironment.Sun.color = _savedEnvironment.SunColor;
    }

    private static Light FindDirectionalLight()
    {
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
                return light;
        }

        return null;
    }

    // Sahnedeki (ve sonradan doğan) renderer'ları tarar: opak kaynak-shader materyalleri toon kopyalarıyla değişir.
    private void ConvertRenderers()
    {
        // Yok olmuş renderer'ların kaydı silinir.
        _deadRenderers.Clear();
        foreach (var renderer in _originalsByRenderer.Keys)
        {
            if (renderer == null)
                _deadRenderers.Add(renderer);
        }

        foreach (var renderer in _deadRenderers)
            _originalsByRenderer.Remove(renderer);

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                continue;

            var materials = renderer.sharedMaterials;
            Material[] converted = null;
            bool masked = _settings.simplifyCharacterOutlines && IsCharacter(renderer);
            for (int i = 0; i < materials.Length; i++)
            {
                var toon = GetToonMaterial(materials[i], masked);
                if (toon == null)
                    continue;

                converted ??= (Material[])materials.Clone();
                converted[i] = toon;
            }

            if (converted == null)
                continue;

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
    }

    // Oyuncu karakteri ya da müşteri mi (konturu sadeleştirilecek küçük, ayrıntılı modeller).
    private static bool IsCharacter(Renderer renderer)
    {
        return renderer.GetComponentInParent<PlayerCharacterVisual>() != null
            || renderer.GetComponentInParent<Customer>() != null;
    }

    // Opak kaynak-shader materyali için (bir kez üretilen) toon kopyası; çevrilmeyecekse null.
    // masked: kontur maskesi açık kopya (yalnızca dış hat çizilir).
    private Material GetToonMaterial(Material original, bool masked)
    {
        if (original == null || original.shader != _sourceShader || original.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.AlphaTest)
            return null;

        var cache = masked ? _maskedToonByOriginal : _toonByOriginal;
        if (cache.TryGetValue(original, out var toon) && toon != null)
            return toon;

        toon = new Material(_settings.toonShader) { name = original.name + " (toon)", hideFlags = HideFlags.DontSave };
        if (original.HasProperty(BaseMapId))
        {
            toon.SetTexture(BaseMapId, original.GetTexture(BaseMapId));
            toon.SetTextureScale(BaseMapId, original.GetTextureScale(BaseMapId));
            toon.SetTextureOffset(BaseMapId, original.GetTextureOffset(BaseMapId));
        }

        if (original.HasProperty(BaseColorId))
            toon.SetColor(BaseColorId, original.GetColor(BaseColorId));

        toon.SetTexture(RampId, _settings.ramp);
        toon.SetColor(ShadowColorId, _settings.shadowColor);
        toon.SetFloat(AmbientStrengthId, _settings.ambientStrength);
        toon.SetColor(RimColorId, _settings.rimColor);
        toon.SetFloat(RimIntensityId, _settings.rimIntensity);
        toon.SetFloat(RimPowerId, _settings.rimPower);
        toon.SetFloat(OutlineMaskId, masked ? 1f : 0f);
        toon.enableInstancing = original.enableInstancing;
        cache[original] = toon;
        return toon;
    }
}
