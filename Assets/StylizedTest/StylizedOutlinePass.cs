using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// DENEME — kontur çizgilerini çizen render geçişi. Renderer asset'ine feature olarak EKLENMEZ (proje ayarı
// değişmesin diye): StylizedLookController görünüm açıkken her kare kameranın renderer'ına kuyruğa ekler.
// Opak nesnelerden ve gökyüzünden sonra, saydamlardan önce çalışır; derinlik + normal dokularını ister.
public class StylizedOutlinePass : ScriptableRenderPass
{
    private class PassData
    {
        public Material Material;
    }

    private readonly Material _material;

    public StylizedOutlinePass(Material material)
    {
        _material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resources = frameData.Get<UniversalResourceData>();

        using var builder = renderGraph.AddRasterRenderPass<PassData>("Stylized Outline", out var passData);
        passData.Material = _material;
        builder.SetRenderAttachment(resources.activeColorTexture, 0);
        // Stencil okunur (toon shader'ın yazdığı kontur maskesi): derinlik-stencil hedefi salt okunur bağlanır.
        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
        if (resources.cameraDepthTexture.IsValid())
            builder.UseTexture(resources.cameraDepthTexture);
        if (resources.cameraNormalsTexture.IsValid())
            builder.UseTexture(resources.cameraNormalsTexture);
        // Shader derinlik/normal dokularını genel (global) adlarıyla okur.
        builder.UseAllGlobalTextures(true);
        builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
        {
            // Geçiş 0: maskesiz pikseller (tüm kenarlar); geçiş 1: maskeli pikseller (yalnızca dış hat).
            context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3);
            context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 1, MeshTopology.Triangles, 3);
        });
    }
}
