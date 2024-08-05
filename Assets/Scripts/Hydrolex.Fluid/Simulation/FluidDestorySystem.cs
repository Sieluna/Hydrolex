using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[DisableAutoCreation]
public partial struct FluidDestorySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<FluidParticle, BoundaryParticle>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<FluidParticle>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        foreach (var (_, entity) in SystemAPI.Query<RefRO<BoundaryParticle>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}