using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class FluidRenderBakingSystem : SystemBase
{
    public ComputeBuffer ParticleBuffer { get; private set; }

    protected override void OnCreate()
    {
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FluidParticle, PhysicsVelocity, LocalToWorld>().Build());
    }

    protected override void OnUpdate()
    {
        using var fluidParticles = new NativeList<FluidParticlePayload>(65535, Allocator.TempJob);

        foreach (var (particle, velocity, transform) in SystemAPI.Query<RefRO<FluidParticle>, RefRO<PhysicsVelocity>, RefRO<LocalToWorld>>())
        {
            fluidParticles.Add(new FluidParticlePayload
            {
                Position = transform.ValueRO.Position,
                Density = particle.ValueRO.RestDensity,
                Velocity = velocity.ValueRO.Linear
            });
        }

        if (ParticleBuffer == null || ParticleBuffer.count != fluidParticles.Length)
        {
            ParticleBuffer?.Release();
            ParticleBuffer = new ComputeBuffer(fluidParticles.Length, Marshal.SizeOf<FluidParticlePayload>());
        }

        ParticleBuffer.SetData(fluidParticles.AsArray());
    }

    protected override void OnDestroy()
    {
        ParticleBuffer?.Dispose();
    }
}