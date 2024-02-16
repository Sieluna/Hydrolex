// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Rendering;
// using Unity.Transforms;
//
// [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
// public partial struct FluidBakingSystem : ISystem
// {
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<BoundaryBakingData>();
//     }
//
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         var ecb = new EntityCommandBuffer(Allocator.Temp);
//
//         foreach (var (localToWorld, bounds, boundary, entity) in SystemAPI
//                      .Query<LocalToWorld, RenderBounds, BoundaryBakingData>()
//                      .WithAll<Boundary>()
//                      .WithEntityAccess())
//         {
//             ParticlesUtilities.CreateParticles(bounds.Value,
//                 localToWorld.Value,
//                 state.EntityManager.GetComponentData<BoundaryParticle>(boundary.Prefab).Radius,
//                 ecb,
//                 boundary.Prefab);
//
//             ecb.RemoveComponent<BoundaryBakingData>(entity);
//         }
//
//         ecb.Playback(state.EntityManager);
//         ecb.Dispose();
//     }
// }