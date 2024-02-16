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
                     .Query<LocalToWorld, RenderBounds, Fluid>()
                     .WithEntityAccess())
        {
            state.Dependency = ParticlesUtilities.CreateParticles(bounds.Value,
                localToWorld.Value,
                state.EntityManager.GetComponentData<FluidParticle>(fluid.Prefab).Radius,
                ecb,
                fluid.Prefab,
                state.Dependency);

            state.Dependency.Complete();

            ecb.DestroyEntity(entity);
        }

        foreach (var (localToWorld, bounds, boundary, entity) in SystemAPI
                     .Query<LocalToWorld, RenderBounds, Boundary>()
                     .WithEntityAccess())
        {
            state.Dependency = ParticlesUtilities.CreateParticles(bounds.Value,
                localToWorld.Value,
                state.EntityManager.GetComponentData<BoundaryParticle>(boundary.Prefab).Radius,
                ecb,
                boundary.Prefab,
                state.Dependency);

            state.Dependency.Complete();

            ecb.RemoveComponent<Boundary>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}