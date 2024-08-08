using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FluidRenderBakingPass : ScriptableRenderPass
{
    private static readonly int s_ScreenSize = Shader.PropertyToID("screenSize");

    private static readonly int s_ViewProjectionMatrix = Shader.PropertyToID("viewProjectionMatrix");

    private static readonly int s_FluidParticles = Shader.PropertyToID("fluidParticles");
    private static readonly int s_FluidParticleCount = Shader.PropertyToID("fluidParticleCount");
    private static readonly int s_FluidParticleRadius = Shader.PropertyToID("fluidParticleRadius");

    private static readonly int s_PositionBuffer = Shader.PropertyToID("positionBuffer");
    private static readonly int s_DepthBuffer = Shader.PropertyToID("depthBuffer");

    private FluidRenderBakingSystem m_FluidRenderBakingSystem;
    private ComputeShader m_FluidRenderBakingShader;

    private RTHandle m_FluidPositionHandle;
    private RTHandle m_FluidDepthHandle;

    public FluidRenderBakingPass(ComputeShader shader)
    {
        m_FluidRenderBakingShader = shader;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        var positionTargetDescriptor = new RenderTextureDescriptor(cameraTargetDescriptor.width,
            cameraTargetDescriptor.height,
            RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidPositionHandle, positionTargetDescriptor, name: "_FluidPositionRT");

        var depthTargetDescriptor = new RenderTextureDescriptor(cameraTargetDescriptor.width,
            cameraTargetDescriptor.height,
            RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidDepthHandle, depthTargetDescriptor, name: "_FluidDepthRT");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_FluidRenderBakingSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FluidRenderBakingSystem>();
        var particleBuffer = m_FluidRenderBakingSystem?.ParticleBuffer ?? new ComputeBuffer(1, Marshal.SizeOf<FluidParticlePayload>());

        var cmd = CommandBufferPool.Get(nameof(FluidRenderBakingPass));

        ref var cameraData = ref renderingData.cameraData;
        var camera = cameraData.camera;

        var scaledCameraWidth = (float)cameraData.cameraTargetDescriptor.width;
        var scaledCameraHeight = (float)cameraData.cameraTargetDescriptor.height;
        if (camera.allowDynamicResolution)
        {
            scaledCameraWidth *= ScalableBufferManager.widthScaleFactor;
            scaledCameraHeight *= ScalableBufferManager.heightScaleFactor;
        }

        var viewMatrix = cameraData.GetViewMatrix();
        var projectionMatrix = cameraData.GetGPUProjectionMatrix();
        var viewAndProjectionMatrix = projectionMatrix * viewMatrix;

        var kernelHandle = m_FluidRenderBakingShader.FindKernel("CSMain");

        cmd.SetComputeVectorParam(m_FluidRenderBakingShader, s_ScreenSize, new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f / scaledCameraWidth, 1.0f / scaledCameraHeight));

        cmd.SetComputeMatrixParam(m_FluidRenderBakingShader, s_ViewProjectionMatrix, viewAndProjectionMatrix);

        cmd.SetComputeBufferParam(m_FluidRenderBakingShader, kernelHandle, s_FluidParticles, particleBuffer);
        cmd.SetComputeIntParam(m_FluidRenderBakingShader, s_FluidParticleCount, particleBuffer.count);
        cmd.SetComputeFloatParam(m_FluidRenderBakingShader, s_FluidParticleRadius, 0.5f);

        cmd.SetComputeTextureParam(m_FluidRenderBakingShader, kernelHandle, s_PositionBuffer, m_FluidPositionHandle);
        cmd.SetComputeTextureParam(m_FluidRenderBakingShader, kernelHandle, s_DepthBuffer, m_FluidDepthHandle);

        var threadGroups = Mathf.CeilToInt(particleBuffer.count / 64.0f);

        cmd.DispatchCompute(m_FluidRenderBakingShader, kernelHandle, threadGroups, 1, 1);

        cmd.SetRenderTarget(m_FluidPositionHandle);
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.SetRenderTarget(m_FluidDepthHandle);
        cmd.ClearRenderTarget(true, true, Color.clear);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        m_FluidPositionHandle?.Release();
        m_FluidDepthHandle?.Release();
    }
}

public class FluidRenderPass : ScriptableRenderPass
{
    private Material m_FluidRenderMaterial;
    private RTHandle m_FluidRenderHandle;

    public FluidRenderPass(Material material)
    {
        m_FluidRenderMaterial = material;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidRenderHandle, cameraTextureDescriptor, name: "_FluidRenderRT");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get(nameof(FluidRenderPass));

        var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

        Blitter.BlitCameraTexture(cmd, cameraTarget, m_FluidRenderHandle, m_FluidRenderMaterial, 0);
        Blitter.BlitCameraTexture(cmd, m_FluidRenderHandle, cameraTarget);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        m_FluidRenderHandle?.Release();
    }
}