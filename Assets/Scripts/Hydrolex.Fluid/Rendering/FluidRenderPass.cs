using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FluidRenderBakingPass : ScriptableRenderPass
{
    private static readonly int s_ScreenSize = Shader.PropertyToID("_ScreenSize");

    private static readonly int s_ViewMatrix = Shader.PropertyToID("_ViewMatrix");
    private static readonly int s_ProjectionMatrix = Shader.PropertyToID("_ProjectionMatrix");
    private static readonly int s_ViewProjectionMatrix = Shader.PropertyToID("_ViewProjectionMatrix");

    private static readonly int s_ProjectionParams = Shader.PropertyToID("_ProjectionParams");
    private static readonly int s_ZBufferParams = Shader.PropertyToID("_ZBufferParams");

    private static readonly int s_FluidParticles = Shader.PropertyToID("_FluidParticles");
    private static readonly int s_FluidParticleCount = Shader.PropertyToID("_FluidParticleCount");
    private static readonly int s_FluidParticleRadius = Shader.PropertyToID("_FluidParticleRadius");

    private static readonly int s_FluidDepthTexture = Shader.PropertyToID("_FluidDepthTexture");
    private static readonly int s_FluidSmoothDepthTexture = Shader.PropertyToID("_FluidSmoothDepthTexture");

    private FluidRenderBakingSystem m_FluidRenderBakingSystem;
    private ComputeShader m_FluidRenderBakingShader;

    private int m_BakingDepthKernelHandle;
    private int m_SmoothingDepthKernelHandle;

    private RTHandle m_FluidDepthHandle;
    private RTHandle m_FluidSmoothDepthHandle;

    public FluidRenderBakingPass(ComputeShader shader)
    {
        m_FluidRenderBakingShader = shader;
        m_BakingDepthKernelHandle = m_FluidRenderBakingShader.FindKernel("BakingDepth");
        m_SmoothingDepthKernelHandle = m_FluidRenderBakingShader.FindKernel("SmoothingDepth");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        var depthTargetDescriptor = new RenderTextureDescriptor(cameraTargetDescriptor.width,
            cameraTargetDescriptor.height,
            RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true
        };
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidDepthHandle, depthTargetDescriptor, name: "_FluidDepthRT");

        var blurDepthTargetDescriptor = new RenderTextureDescriptor(cameraTargetDescriptor.width,
            cameraTargetDescriptor.height,
            RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true
        };
        RenderingUtils.ReAllocateIfNeeded(ref m_FluidSmoothDepthHandle, blurDepthTargetDescriptor, name: "_FluidSmoothDepthRT");

        FluidRenderFeature.Instance.RTHandleDictionary["FluidDepth"] = m_FluidDepthHandle;
        FluidRenderFeature.Instance.RTHandleDictionary["FluidSmoothDepth"] = m_FluidSmoothDepthHandle;
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

        var isTargetFlipped = cameraData.IsCameraProjectionMatrixFlipped();

        var near = camera.nearClipPlane;
        var far = camera.farClipPlane;
        var invNear = Mathf.Approximately(near, 0.0f) ? 0.0f : 1.0f / near;
        var invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;

        var zc0 = 1.0f - far * invNear;
        var zc1 = far * invNear;

        var zBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);

        if (SystemInfo.usesReversedZBuffer)
        {
            zBufferParams.y += zBufferParams.x;
            zBufferParams.x = -zBufferParams.x;
            zBufferParams.w += zBufferParams.z;
            zBufferParams.z = -zBufferParams.z;
        }

        var viewMatrix = cameraData.GetViewMatrix();
        var projectionMatrix = cameraData.GetGPUProjectionMatrix();
        var viewAndProjectionMatrix = projectionMatrix * viewMatrix;

        cmd.SetRenderTarget(m_FluidDepthHandle);
        cmd.ClearRenderTarget(true, true, Color.clear);

        cmd.SetRenderTarget(m_FluidSmoothDepthHandle);
        cmd.ClearRenderTarget(true, true, Color.clear);

        cmd.SetComputeVectorParam(m_FluidRenderBakingShader, s_ScreenSize, new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f / scaledCameraWidth, 1.0f / scaledCameraHeight));

        cmd.SetComputeMatrixParam(m_FluidRenderBakingShader, s_ViewMatrix, viewMatrix);
        cmd.SetComputeMatrixParam(m_FluidRenderBakingShader, s_ProjectionMatrix, projectionMatrix);
        cmd.SetComputeMatrixParam(m_FluidRenderBakingShader, s_ViewProjectionMatrix, viewAndProjectionMatrix);

        cmd.SetComputeVectorParam(m_FluidRenderBakingShader, s_ProjectionParams, new Vector4(isTargetFlipped ? -1.0f : 1.0f, near, far, 1.0f * invFar));
        cmd.SetComputeVectorParam(m_FluidRenderBakingShader, s_ZBufferParams, zBufferParams);

        cmd.SetComputeBufferParam(m_FluidRenderBakingShader, m_BakingDepthKernelHandle, s_FluidParticles, particleBuffer);
        cmd.SetComputeIntParam(m_FluidRenderBakingShader, s_FluidParticleCount, particleBuffer.count);
        cmd.SetComputeFloatParam(m_FluidRenderBakingShader, s_FluidParticleRadius, 0.2f);

        cmd.SetComputeTextureParam(m_FluidRenderBakingShader, m_BakingDepthKernelHandle, s_FluidDepthTexture, m_FluidDepthHandle);

        var threadGroups = Mathf.CeilToInt(particleBuffer.count / 64.0f);

        cmd.DispatchCompute(m_FluidRenderBakingShader, m_BakingDepthKernelHandle, threadGroups, 1, 1);

        cmd.SetComputeTextureParam(m_FluidRenderBakingShader, m_SmoothingDepthKernelHandle, s_FluidDepthTexture, m_FluidDepthHandle);
        cmd.SetComputeTextureParam(m_FluidRenderBakingShader, m_SmoothingDepthKernelHandle, s_FluidSmoothDepthTexture, m_FluidSmoothDepthHandle);

        var threadGroupsX = Mathf.CeilToInt(scaledCameraWidth / 8.0f);
        var threadGroupsY = Mathf.CeilToInt(scaledCameraHeight / 8.0f);
        cmd.DispatchCompute(m_FluidRenderBakingShader, m_SmoothingDepthKernelHandle, threadGroupsX, threadGroupsY, 1);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        m_FluidDepthHandle?.Release();
        m_FluidSmoothDepthHandle?.Release();
    }
}

public class FluidRenderPass : ScriptableRenderPass
{
    private static readonly int s_FluidDepthTexture = Shader.PropertyToID("_FluidDepthTexture");

    private Material m_FluidRenderMaterial;

    private RTHandle m_FluidDepthHandle;
    private RTHandle m_FluidRenderHandle;

    public FluidRenderPass(Material material)
    {
        m_FluidRenderMaterial = material;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        cameraTextureDescriptor.depthBufferBits = (int)DepthBits.None;

        RenderingUtils.ReAllocateIfNeeded(ref m_FluidRenderHandle, cameraTextureDescriptor, name: "_FluidRenderRT");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        m_FluidDepthHandle ??= FluidRenderFeature.Instance.RTHandleDictionary["FluidSmoothDepth"];

        var cmd = CommandBufferPool.Get(nameof(FluidRenderPass));

        var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

        m_FluidRenderMaterial.SetTexture(s_FluidDepthTexture, m_FluidDepthHandle);

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