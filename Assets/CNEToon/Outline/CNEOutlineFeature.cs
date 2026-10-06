using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// CNE outline: Outline rendering layer'ındaki nesnelere (karakterler, oynanış nesneleri) ekran uzayında çizgi çizer;
// mimari çizgi almaz. Üç adım: (a) katmandaki nesneler kamera derinliğine karşı tek kanallı bir maskeye çizilir,
// (b) derinlik + normal dokularında kenar bulunur, (c) çizgi yalnızca maskenin dokunduğu yerde sahneye bindirilir.
//
// Sabit bir STİLDİR: oyun durumuna göre nesne nesne açılıp kapanmaz (GDD 4.1.2 — nesnenin kendisi vurgulanmaz).
// Yalnızca eklendiği renderer'da çalışır; Şef'in kör görüş renderer'ına EKLENMEZ ve o renderer'ın geçişlerine
// dokunmaz. Bütün değerler bu feature'ın ayarlarıdır; koda gömülü değer yoktur.
public class CNEOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Çizgi alacak renderer'ların rendering layer'ı (Tags and Layers → Rendering Layers: Outline).")]
        public RenderingLayerMask outlineLayer;
        [Tooltip("Geçişin sırası. Gökyüzünden sonra: saydamlar ve efektler çizginin üstünde kalır, AA çizgiyi de yumuşatır.")]
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

        [Header("Çizgi")]
        [Tooltip("Çizgi rengi (karışım 1 iken düz bu renk).")]
        public Color outlineColor = new Color32(0x24, 0x23, 0x3A, 0xFF);
        [Tooltip("1 = düz çizgi rengi; 0 = nesnenin kendi renginin koyu tonu.")]
        [Range(0f, 1f)] public float colorBlend = 1f;
        [Tooltip("Karışım 0'a yaklaşırken sahne renginin çarpıldığı koyuluk.")]
        [Range(0f, 1f)] public float darken = 0.35f;
        [Tooltip("Çizgi kalınlığı, referans yükseklikte piksel. Ekran yüksekliğiyle ölçeklenir.")]
        [Min(0.5f)] public float widthPx = 2f;
        [Tooltip("Kalınlığın tanımlandığı ekran yüksekliği (piksel).")]
        [Min(1f)] public float referenceHeight = 1080f;

        [Header("Kenar bulma")]
        [Tooltip("Derinlik farkı eşiği (mesafeye oranla). Küçük = daha çok siluet çizgisi.")]
        [Min(0.0001f)] public float depthThreshold = 0.05f;
        [Tooltip("Yüzey yönü farkı eşiği. Küçük = yumuşak kıvrımlar da çizilir; büyük = yalnız keskin kırımlar.")]
        [Min(0.0001f)] public float normalThreshold = 0.5f;
        [Tooltip("Kenar geçişinin yumuşaklığı (eşiğin katı). 0 = sert kenar.")]
        [Range(0f, 2f)] public float edgeSoftness = 0.5f;

        [Header("Mesafeyle solma (m)")]
        [Min(0f)] public float fadeStart = 12f;
        [Min(0f)] public float fadeEnd = 25f;

        [Header("Shader'lar (elle değiştirilmez)")]
        public Shader maskShader;
        public Shader compositeShader;
    }

    // Çizgiyi çalışırken açıp kapatır (görünüm seçici, debug tuşu). Sabit stil: nesne başına değil, tümü birden.
    public static bool Enabled { get; set; } = true;

    [SerializeField] private Settings settings = new();

    private Material _maskMaterial;
    private Material _compositeMaterial;
    private OutlinePass _pass;

    public override void Create()
    {
        if (settings.maskShader != null && _maskMaterial == null)
            _maskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
        if (settings.compositeShader != null && _compositeMaterial == null)
            _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);

        _pass = new OutlinePass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Enabled || _maskMaterial == null || _compositeMaterial == null || settings.outlineLayer.value == 0)
            return;

        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        _pass.Setup(settings, _maskMaterial, _compositeMaterial);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);
        _maskMaterial = null;
        _compositeMaterial = null;
    }

    private class OutlinePass : ScriptableRenderPass
    {
        private static readonly System.Collections.Generic.List<ShaderTagId> ShaderTags = new()
        {
            new("UniversalForward"), new("UniversalForwardOnly"), new("SRPDefaultUnlit"),
        };

        private static readonly int MaskId = Shader.PropertyToID("_CNEOutlineMask");
        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int DarkenId = Shader.PropertyToID("_OutlineDarken");
        private static readonly int ColorBlendId = Shader.PropertyToID("_OutlineColorBlend");
        private static readonly int WidthId = Shader.PropertyToID("_OutlineWidthPx");
        private static readonly int ReferenceHeightId = Shader.PropertyToID("_ReferenceHeight");
        private static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThresholdId = Shader.PropertyToID("_NormalThreshold");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int FadeStartId = Shader.PropertyToID("_FadeStart");
        private static readonly int FadeEndId = Shader.PropertyToID("_FadeEnd");

        private static readonly MaterialPropertyBlock PropertyBlock = new();

        private Settings _settings;
        private Material _maskMaterial;
        private Material _compositeMaterial;

        private class MaskPassData
        {
            public RendererListHandle RendererList;
        }

        private class CompositePassData
        {
            public Material Material;
            public TextureHandle Mask;
        }

        public OutlinePass()
        {
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void Setup(Settings settings, Material maskMaterial, Material compositeMaterial)
        {
            _settings = settings;
            _maskMaterial = maskMaterial;
            _compositeMaterial = compositeMaterial;
            renderPassEvent = settings.passEvent;

            // Shader'da Properties bloğu olmadığı için Unity rengi kendiliğinden linear'a çevirmez.
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            compositeMaterial.SetColor(ColorId, linear ? settings.outlineColor.linear : settings.outlineColor);
            compositeMaterial.SetFloat(DarkenId, settings.darken);
            compositeMaterial.SetFloat(ColorBlendId, settings.colorBlend);
            compositeMaterial.SetFloat(WidthId, settings.widthPx);
            compositeMaterial.SetFloat(ReferenceHeightId, settings.referenceHeight);
            compositeMaterial.SetFloat(DepthThresholdId, settings.depthThreshold);
            compositeMaterial.SetFloat(NormalThresholdId, settings.normalThreshold);
            compositeMaterial.SetFloat(EdgeSoftnessId, settings.edgeSoftness);
            compositeMaterial.SetFloat(FadeStartId, settings.fadeStart);
            compositeMaterial.SetFloat(FadeEndId, Mathf.Max(settings.fadeEnd, settings.fadeStart + 0.01f));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            // (a) Maske: Outline katmanındaki opak nesneler, kameranın derinliğine karşı.
            var maskDesc = renderGraph.GetTextureDesc(resources.activeColorTexture);
            maskDesc.name = "_CNEOutlineMask";
            maskDesc.format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm;
            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.clear;
            TextureHandle mask = renderGraph.CreateTexture(maskDesc);

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("CNE Outline Mask", out var passData))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(
                    ShaderTags, renderingData, cameraData, lightData,
                    cameraData.defaultOpaqueSortFlags);
                drawing.overrideMaterial = _maskMaterial;
                drawing.overrideMaterialPassIndex = 0;

                var filtering = new FilteringSettings(RenderQueueRange.opaque, -1, _settings.outlineLayer.value);
                passData.RendererList = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawing, filtering));

                builder.UseRendererList(passData.RendererList);
                builder.SetRenderAttachment(mask, 0);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc((MaskPassData data, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(data.RendererList));
            }

            // (b) + (c) Kenar bulma ve bindirme.
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("CNE Outline Composite", out var passData))
            {
                passData.Material = _compositeMaterial;
                passData.Mask = mask;

                builder.UseTexture(mask);
                if (resources.cameraDepthTexture.IsValid())
                    builder.UseTexture(resources.cameraDepthTexture);
                if (resources.cameraNormalsTexture.IsValid())
                    builder.UseTexture(resources.cameraNormalsTexture);
                // Shader derinlik/normal dokularını genel (global) adlarıyla okur.
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachment(resources.activeColorTexture, 0);
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                {
                    PropertyBlock.Clear();
                    PropertyBlock.SetTexture(MaskId, data.Mask);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1, PropertyBlock);
                });
            }
        }
    }
}
