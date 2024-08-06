using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct FluidData
{
    public float3 Position;
    public float Radius;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class FluidRenderBakingSystem : SystemBase
{
    private static readonly int s_FluidParticles = Shader.PropertyToID("FluidParticles");
    private static readonly int s_FluidParticleCount = Shader.PropertyToID("FluidParticleCount");

    private ComputeBuffer m_ParticleBuffer;
    private Material m_FluidMaterial;

    protected override void OnCreate()
    {
        m_FluidMaterial = FluidRenderFeature.Instance.FluidRenderMaterial;

        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FluidParticle, LocalToWorld>().Build());
    }

    protected override void OnUpdate()
    {
        using var fluidParticles = new NativeList<FluidData>(65535, Allocator.TempJob);

        foreach (var (particle, transform) in SystemAPI.Query<RefRO<FluidParticle>, RefRO<LocalToWorld>>())
        {
            fluidParticles.Add(new FluidData
            {
                Position = transform.ValueRO.Position,
                Radius = particle.ValueRO.Radius
            });
        }

        if (m_ParticleBuffer == null || m_ParticleBuffer.count != fluidParticles.Length)
        {
            m_ParticleBuffer?.Release();
            m_ParticleBuffer = new ComputeBuffer(fluidParticles.Length, sizeof(float) * 4);
        }

        m_ParticleBuffer.SetData(fluidParticles.AsArray());

        m_FluidMaterial.SetBuffer(s_FluidParticles, m_ParticleBuffer);
        m_FluidMaterial.SetInt(s_FluidParticleCount, fluidParticles.Length);
    }

    protected override void OnDestroy()
    {
        m_ParticleBuffer?.Dispose();
    }
}