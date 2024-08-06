using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FluidRenderPass : ScriptableRenderPass
{
    private Material m_FluidRenderMaterial;
    private RTHandle m_FluidRenderTarget;

    public FluidRenderPass(Material material)
    {
        m_FluidRenderMaterial = material;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        cameraTextureDescriptor.depthBufferBits = (int)DepthBits.None;
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidRenderTarget, cameraTextureDescriptor, name: "_FluidRenderRT");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get(nameof(FluidRenderPass));

        var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

        Blitter.BlitCameraTexture(cmd, source, m_FluidRenderTarget, m_FluidRenderMaterial, 0);
        Blitter.BlitCameraTexture(cmd, m_FluidRenderTarget, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        m_FluidRenderTarget?.Release();
    }
}