using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public partial struct FluidGenerateSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<Fluid, Boundary>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (localToWorld, bounds, fluid, entity) in SystemAPI
                     .Query<RefRO<LocalToWorld>, RefRO<RenderBounds>, RefRO<Fluid>>()
                     .WithEntityAccess())
        {
            state.Dependency = ParticlesUtilities.CreateParticles(bounds.ValueRO.Value,
                localToWorld.ValueRO.Value,
                state.EntityManager.GetComponentData<FluidParticle>(fluid.ValueRO.Prefab).Radius,
                ecb,
                fluid.ValueRO.Prefab,
                state.Dependency);

            state.Dependency.Complete();

            ecb.DestroyEntity(entity);
        }

        foreach (var (localToWorld, bounds, boundary, entity) in SystemAPI
                     .Query<RefRO<LocalToWorld>, RefRO<RenderBounds>, RefRO<Boundary>>()
                     .WithEntityAccess())
        {
            state.Dependency = ParticlesUtilities.CreateParticles(bounds.ValueRO.Value,
                localToWorld.ValueRO.Value,
                state.EntityManager.GetComponentData<BoundaryParticle>(boundary.ValueRO.Prefab).Radius,
                ecb,
                boundary.ValueRO.Prefab,
                state.Dependency);

            state.Dependency.Complete();

            ecb.RemoveComponent<Boundary>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}