using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[StructLayout(LayoutKind.Sequential)]
public struct FluidParticlePayload
{
    public float3 Position;
    public float Density;
    public float3 Velocity;
}

public class FluidRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent FluidBakingRenderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    public RenderPassEvent FluidRenderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    public ComputeShader FluidRenderBakingShader;
    public Shader FluidRenderShader;

    private FluidRenderBakingPass m_FluidRenderBakingPass;
    private FluidRenderPass m_fluidRenderPass;
    private Material m_FluidRenderMaterial;

    public Dictionary<string, RTHandle> RTHandleDictionary { get; } = new();

    public static FluidRenderFeature Instance { get; private set; }

    public override void Create()
    {
        m_FluidRenderMaterial ??= CoreUtils.CreateEngineMaterial(FluidRenderShader);

        m_FluidRenderBakingPass = new FluidRenderBakingPass(FluidRenderBakingShader)
        {
            renderPassEvent = FluidBakingRenderPassEvent
        };
        m_fluidRenderPass = new FluidRenderPass(m_FluidRenderMaterial)
        {
            renderPassEvent = FluidRenderPassEvent
        };

        Instance = this;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if ((renderingData.cameraData.cameraType & ~(CameraType.Game | CameraType.SceneView)) == 0)
        {
            renderer.EnqueuePass(m_FluidRenderBakingPass);
            renderer.EnqueuePass(m_fluidRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_FluidRenderBakingPass?.Dispose();
        m_fluidRenderPass?.Dispose();
    }
}