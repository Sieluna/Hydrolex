using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FluidRenderFeature : ScriptableRendererFeature
{
    public static FluidRenderFeature Instance { get; private set; }

    [SerializeField] private RenderPassEvent m_RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    [SerializeField] private Mesh m_SphereMesh;
    [SerializeField] private Shader m_FluidRenderShader;

    private FluidRenderPass m_fluidRenderPass;

    public Material FluidRenderMaterial { get; private set; }

    public override void Create()
    {
        FluidRenderMaterial ??= CoreUtils.CreateEngineMaterial(m_FluidRenderShader);

        m_fluidRenderPass = new FluidRenderPass(FluidRenderMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };

        Instance = this;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_fluidRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_fluidRenderPass.Dispose();
    }
}