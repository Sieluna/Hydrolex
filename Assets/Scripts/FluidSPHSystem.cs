using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial struct FluidInitializeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAny<Fluid, Boundary>().Build());
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (localToWorld, bounds, fluid, entity) in SystemAPI
                     .Query<LocalToWorld, RenderBounds, Fluid>()
                     .WithAll<Fluid>()
                     .WithEntityAccess())
        {
            CreateParticles(localToWorld.Value,
                bounds.Value,
                state.EntityManager.GetComponentData<FluidParticle>(fluid.Prefab).Radius,
                ecb,
                fluid.Prefab);

            ecb.DestroyEntity(entity);
        }

        foreach (var (localToWorld, bounds, boundary, entity) in SystemAPI
                     .Query<LocalToWorld, RenderBounds, Boundary>()
                     .WithAll<Boundary>()
                     .WithEntityAccess())
        {
            CreateParticles(localToWorld.Value,
                bounds.Value,
                state.EntityManager.GetComponentData<BoundaryParticle>(boundary.Prefab).Radius,
                ecb,
                boundary.Prefab);

            ecb.RemoveComponent<Boundary>(entity);
        }
    }

    private static void CreateParticles(float4x4 localToWorld, AABB bounds, float radius, EntityCommandBuffer ecb, Entity prefab)
    {
        var scale = new float3(math.length(localToWorld.c0.xyz), math.length(localToWorld.c1.xyz), math.length(localToWorld.c2.xyz));

        var scaledRadius = radius / scale;
        var numParticles = math.ceil(bounds.Size / (2 * scaledRadius));

        for (var z = 0; z < numParticles.z; z++)
        {
            for (var y = 0; y < numParticles.y; y++)
            {
                for (var x = 0; x < numParticles.x; x++)
                {
                    var position = math.transform(localToWorld, new float3(x , y , z) * 2 * scaledRadius + bounds.Min + scaledRadius);

                    var entity = ecb.Instantiate(prefab);

                    ecb.SetComponent(entity, LocalTransform.FromPosition(position));
                }
            }
        }
    }
}

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct FluidSPHSystem : ISystem
{
    private const float k_KernelRadiusRate = 4f;
    private const int k_Concurrency = 32;

    private ComponentTypeHandle<FluidParticle> m_FluidParticleTypeHandle;
    private ComponentTypeHandle<PhysicsMass> m_PhysicsMassTypeHandle;
    private ComponentTypeHandle<PhysicsVelocity> m_PhysicsVelocityTypeHandle;

    private EntityQuery m_ParticleQuery;
    private EntityQuery m_BoundaryQuery;

    public void OnCreate(ref SystemState state)
    {
        m_FluidParticleTypeHandle = state.GetComponentTypeHandle<FluidParticle>(true);
        m_PhysicsMassTypeHandle = state.GetComponentTypeHandle<PhysicsMass>(true);
        m_PhysicsVelocityTypeHandle = state.GetComponentTypeHandle<PhysicsVelocity>();

        m_ParticleQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<FluidParticle, LocalTransform, PhysicsVelocity, PhysicsMass, PhysicsCollider>()
            .Build(ref state);
        m_BoundaryQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<BoundaryParticle, LocalTransform>()
            .Build(ref state);

        state.RequireForUpdate(m_ParticleQuery);  
    }

    public void OnUpdate(ref SystemState state)
    {
        m_FluidParticleTypeHandle.Update(ref state);
        m_PhysicsMassTypeHandle.Update(ref state);
        m_PhysicsVelocityTypeHandle.Update(ref state);

        var particleCount = m_ParticleQuery.CalculateEntityCount();
        var boundaryParticleCount = m_BoundaryQuery.CalculateEntityCount();

        var transforms = m_ParticleQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator, out var transformHandle);
        var particles = m_ParticleQuery.ToComponentDataListAsync<FluidParticle>(state.WorldUpdateAllocator, out var particleHandle);
        var physicsMasses = m_ParticleQuery.ToComponentDataListAsync<PhysicsMass>(state.WorldUpdateAllocator, out var physicsMassHandle);
        var physicsVelocities = m_ParticleQuery.ToComponentDataListAsync<PhysicsVelocity>(state.WorldUpdateAllocator, out var physicsVelocityHandle);
        var boundaryTransforms = m_BoundaryQuery.ToComponentDataListAsync<LocalTransform>(state.WorldUpdateAllocator, out var boundaryTransformHandle);
        var boundaryParticles = m_BoundaryQuery.ToComponentDataListAsync<BoundaryParticle>(state.WorldUpdateAllocator, out var boundaryParticleHandle);

        state.Dependency = JobHandle.CombineDependencies(state.Dependency, transformHandle, boundaryTransformHandle);
        state.Dependency = JobHandle.CombineDependencies(state.Dependency, particleHandle, boundaryParticleHandle);
        state.Dependency = JobHandle.CombineDependencies(state.Dependency, physicsMassHandle, physicsVelocityHandle);

        var gridSize = new NativeArray<float>(1, Allocator.TempJob);

        state.Dependency = new FindGridSizeJob { GridSize = gridSize, KernelRadiusRate = k_KernelRadiusRate }
            .ScheduleParallel(m_ParticleQuery, state.Dependency);

        var minMax = new NativeArray<float>(6, Allocator.TempJob);
        minMax[0] = minMax[1] = minMax[2] = float.MaxValue;
        minMax[3] = minMax[4] = minMax[5] = float.MinValue;

        var grid = new NativeParallelMultiHashMap<uint, int>(particleCount, Allocator.TempJob);
        var boundaryGrid = new NativeParallelMultiHashMap<uint, int>(boundaryParticleCount, Allocator.TempJob);

        state.Dependency = new FindBoundsJob
        {
            Transforms = transforms,
            GridSize = gridSize,

            MinMax = minMax,
            Grid = grid.AsParallelWriter()
        }.Schedule(particleCount, k_Concurrency, state.Dependency);

        state.Dependency = new FindBoundsJob
        {
            Transforms = boundaryTransforms,
            GridSize = gridSize,

            MinMax = minMax,
            Grid = boundaryGrid.AsParallelWriter()
        }.Schedule(boundaryParticleCount, k_Concurrency, state.Dependency);

        var pressures = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var densities = new NativeArray<float>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        state.Dependency = new ComputeDensityAndPressureJob
        {
            Transforms = transforms,
            Masses = physicsMasses,
            Particles = particles,
            Grid = grid,
            GridSize = gridSize,
            BoundaryTransforms = boundaryTransforms,
            BoundaryGrid = boundaryGrid,
            KernelRadiusRate = k_KernelRadiusRate,

            Densities = densities,
            Pressures = pressures
        }.Schedule(particleCount, k_Concurrency, state.Dependency);

        var forces = new NativeArray<float3>(particleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        state.Dependency = new ComputeForceJob
        {
            Transforms = transforms,
            Masses = physicsMasses,
            Velocities = physicsVelocities,
            Particles = particles,
            Grid = grid,
            GridSize = gridSize,
            BoundaryTransforms = boundaryTransforms,
            BoundaryParticles = boundaryParticles,
            BoundaryGrid = boundaryGrid,
            Densities = densities,
            Pressures = pressures,
            KernelRadiusRate = k_KernelRadiusRate,

            Forces = forces
        }.Schedule(particleCount, k_Concurrency, state.Dependency);

        var chunkBaseEntityIndices =
            m_ParticleQuery.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, state.Dependency,
                out var baseIndexJobHandle);

        state.Dependency = new ApplyForceJob
        {
            ChunkBaseEntityIndices = chunkBaseEntityIndices,
            Densities = densities,
            Forces = forces,
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(m_ParticleQuery, baseIndexJobHandle);

        state.CompleteDependency();

        transforms.Dispose(state.Dependency);
        particles.Dispose(state.Dependency);
        physicsMasses.Dispose(state.Dependency);
        physicsVelocities.Dispose(state.Dependency);
        boundaryTransforms.Dispose(state.Dependency);
        boundaryParticles.Dispose(state.Dependency);
        gridSize.Dispose(state.Dependency);
        minMax.Dispose(state.Dependency);
        grid.Dispose(state.Dependency);
        boundaryGrid.Dispose(state.Dependency);
        pressures.Dispose(state.Dependency);
        densities.Dispose(state.Dependency);
        forces.Dispose(state.Dependency);
    }

    [BurstCompile]
    private struct ComputeDensityAndPressureJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<LocalTransform> Transforms;
        [ReadOnly] public NativeList<PhysicsMass> Masses;
        [ReadOnly] public NativeList<FluidParticle> Particles;
        [ReadOnly] public NativeParallelMultiHashMap<uint, int> Grid;
        [ReadOnly] public NativeArray<float> GridSize;
        [ReadOnly] public NativeList<LocalTransform> BoundaryTransforms;
        [ReadOnly] public NativeParallelMultiHashMap<uint, int> BoundaryGrid;
        [ReadOnly] public float KernelRadiusRate;

        public NativeArray<float> Densities;
        public NativeArray<float> Pressures;

        public void Execute(int index)
        {
            var density = 0f;
            var particle = Particles[index];
            var position = Transforms[index].Position;
            var radius = particle.Radius;
            var mass = 1f / Masses[index].InverseMass;
            var gasConstant = particle.GasConstant;
            var restDensity = particle.RestDensity;
            var gridPosition = HashUtilities.Quantize(position, GridSize[0]);
            var kernelRadius = radius * KernelRadiusRate;
            var kernelRadius2 = math.pow(kernelRadius, 2f);
            var poly6Constant = mass * 315f / (64f * math.PI * math.pow(kernelRadius, 9f));

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var z = -1; z <= 1; z++)
                    {
                        var neighborGridIndex = HashUtilities.Hash(gridPosition + new int3(x, y, z));
                        var found = Grid.TryGetFirstValue(neighborGridIndex, out var j, out var iterator);
                        while (found)
                        {
                            var distance2 = math.lengthsq(position - Transforms[j].Position);
                            density += ComputeDensity(poly6Constant, kernelRadius2, distance2);

                            found = Grid.TryGetNextValue(out j, ref iterator);
                        }

                        found = BoundaryGrid.TryGetFirstValue(neighborGridIndex, out j, out iterator);
                        while (found)
                        {
                            var distance2 = math.lengthsq(position - BoundaryTransforms[j].Position);
                            density += ComputeDensity(poly6Constant, kernelRadius2, distance2);

                            found = BoundaryGrid.TryGetNextValue(out j, ref iterator);
                        }
                    }
                }
            }

            Densities[index] = density;
            // ideal gas state eq. p_i = k(\rho_i - \rho_0)
            Pressures[index] = math.max(gasConstant * (density - restDensity), 0);
        }

        private static float ComputeDensity(float poly6Constant, float kernelRadius2, float distance2)
        {
            if (distance2 < kernelRadius2)
            {
                // kernel poly6, \rho_i = m \Sigma{W_{poly6}}
                // where W_poly6 = \frac{315}{64 \pi h^9} (h^2 - r^2)^3
                return poly6Constant * math.pow(kernelRadius2 - distance2, 3f);
            }
            return 0;
        }
    }

    [BurstCompile]
    private struct ComputeForceJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<LocalTransform> Transforms;
        [ReadOnly] public NativeList<PhysicsMass> Masses;
        [ReadOnly] public NativeList<PhysicsVelocity> Velocities;
        [ReadOnly] public NativeList<FluidParticle> Particles;
        [ReadOnly] public NativeParallelMultiHashMap<uint, int> Grid;
        [ReadOnly] public NativeArray<float> GridSize;
        [ReadOnly] public NativeList<LocalTransform> BoundaryTransforms;
        [ReadOnly] public NativeList<BoundaryParticle> BoundaryParticles;
        [ReadOnly] public NativeParallelMultiHashMap<uint, int> BoundaryGrid;
        [ReadOnly] public NativeArray<float> Densities;
        [ReadOnly] public NativeArray<float> Pressures;
        [ReadOnly] public float KernelRadiusRate;

        public NativeArray<float3> Forces;

        private struct ParticleData
        {
            public float Density;
            public float Pressure;
            public float3 Velocity;
            public float Mass;
        }

        public void Execute(int index)
        {
            var density = Densities[index];
            var pressure = Pressures[index];

            var position = Transforms[index].Position;
            var velocity = Velocities[index].Linear;

            var pressureForce = new float3();
            var viscosityForce = new float3();
            var gravityForce = new float3(0, -9.81f, 0) * density;

            var kernelRadius = Particles[index].Radius * KernelRadiusRate;
            var viscosityCoefficient = Particles[index].Viscosity;

            var gridPosition = HashUtilities.Quantize(position, GridSize[0]);

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var z = -1; z <= 1; z++)
                    {
                        var neighborGridIndex = HashUtilities.Hash(gridPosition + new int3(x, y, z));
                        var found = Grid.TryGetFirstValue(neighborGridIndex, out var neighbor, out var iterator);
                        while (found)
                        {
                            if (index != neighbor)
                            {
                                var distanceVector = position - Transforms[neighbor].Position;
                                var currentParticle = new ParticleData
                                {
                                    Density = density,
                                    Pressure = pressure,
                                    Velocity = velocity
                                };
                                var neighborParticle = new ParticleData
                                {
                                    Density = Densities[neighbor],
                                    Pressure = Pressures[neighbor],
                                    Velocity = Velocities[neighbor].Linear,
                                    Mass = 1f / Masses[neighbor].InverseMass
                                };

                                var result = ComputeForcePressureAndViscosity(math.length(distanceVector),
                                    kernelRadius,
                                    viscosityCoefficient,
                                    currentParticle,
                                    neighborParticle,
                                    distanceVector);
                                pressureForce += result.c0;
                                viscosityForce += result.c1;
                            }
                            found = Grid.TryGetNextValue(out neighbor, ref iterator);
                        }

                        found = BoundaryGrid.TryGetFirstValue(neighborGridIndex, out neighbor, out iterator);
                        while (found)
                        {
                            var distanceVector = position - BoundaryTransforms[neighbor].Position;
                            var currentParticle = new ParticleData
                            {
                                Density = density,
                                Pressure = pressure,
                                Velocity = velocity
                            };
                            var neighborParticle = new ParticleData
                            {
                                Density = BoundaryParticles[neighbor].RestDensity,
                                Pressure = 0,
                                Velocity = new float3(0),
                                Mass = BoundaryParticles[neighbor].Mass
                            };
                            var result = ComputeForcePressureAndViscosity(math.length(distanceVector),
                                kernelRadius,
                                viscosityCoefficient,
                                currentParticle,
                                neighborParticle,
                                distanceVector);
                            pressureForce += result.c0;
                            viscosityForce += result.c1;

                            found = BoundaryGrid.TryGetNextValue(out neighbor, ref iterator);
                        }

                    }
                }
            }

            Forces[index] = pressureForce + viscosityForce + gravityForce;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3x2 ComputeForcePressureAndViscosity(float distance,
            float kernelRadius,
            float viscosity,
            ParticleData currentParticle,
            ParticleData neighborParticle,
            float3 distanceVector)
        {
            if (distance < kernelRadius)
            {
                // f_{i}^{press} = - \rho_i \Sigma{ m_j (\frac{p_i}{\rho_i^2} + \frac{p_j}{\rho_j^2})\Delta{W_{spiky}}} 
                // where \Delta{W_{spiky}} = - \frac{45}{\pi h^6}(h - r)^2e_r
                // e_r is i - j 
                var pressureComponent = -currentParticle.Density * neighborParticle.Mass *
                                        (currentParticle.Pressure / math.pow(currentParticle.Density, 2f) +
                                         neighborParticle.Pressure / math.pow(neighborParticle.Density, 2f)) *
                                        (-45f / (math.PI * math.pow(kernelRadius, 6f)) *
                                         math.pow(kernelRadius - distance, 2f)) * math.normalize(distanceVector);

                // f_i^{visco} = \frac{\mu}{\rho_i}\Sigma{m_j(u_j - u_i)}\Delta^2W_{visco}
                // where \Delta^2W_{visco}\Delta^2W_{visco} = \frac{45}{\pi h^6}(h - r)
                var viscosityComponent = viscosity / currentParticle.Density * neighborParticle.Mass *
                                         (neighborParticle.Velocity - currentParticle.Velocity) * 45f /
                                         (math.PI * math.pow(kernelRadius, 6f)) *
                                         (kernelRadius - distance);

                return new float3x2 { c0 = pressureComponent, c1 = viscosityComponent };
            }

            return float3x2.zero;
        }
    }
}

[BurstCompile]
public partial struct FindGridSizeJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public NativeArray<float> GridSize;
    public float KernelRadiusRate;

    private void Execute(ref FluidParticle setting)
    {
        var radius = setting.Radius;
        if (radius < GridSize[0] / KernelRadiusRate)
        {
            GridSize[0] = radius * KernelRadiusRate;
        }
    }
}

[BurstCompile]
public struct FindBoundsJob : IJobParallelFor
{
    [ReadOnly] public NativeList<LocalTransform> Transforms;
    [ReadOnly] public NativeArray<float> GridSize;
    [NativeDisableParallelForRestriction] public NativeArray<float> MinMax;
    [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<uint, int>.ParallelWriter Grid;

    public void Execute(int index)
    {
        if (Transforms[index].Position.x < MinMax[0]) MinMax[0] = Transforms[index].Position.x;
        if (Transforms[index].Position.y < MinMax[1]) MinMax[1] = Transforms[index].Position.y;
        if (Transforms[index].Position.z < MinMax[2]) MinMax[2] = Transforms[index].Position.z;

        if (Transforms[index].Position.x > MinMax[3]) MinMax[3] = Transforms[index].Position.x;
        if (Transforms[index].Position.y > MinMax[4]) MinMax[4] = Transforms[index].Position.y;
        if (Transforms[index].Position.z > MinMax[5]) MinMax[5] = Transforms[index].Position.z;

        Grid.Add(HashUtilities.Hash(HashUtilities.Quantize(Transforms[index].Position, GridSize[0])), index);
    }
}

[BurstCompile]
public partial struct ApplyForceJob : IJobEntity
{
    [ReadOnly] public NativeArray<int> ChunkBaseEntityIndices;
    [ReadOnly] public NativeArray<float> Densities;
    [ReadOnly] public NativeArray<float3> Forces;
    [ReadOnly] public float DeltaTime;

    private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, [EntityIndexInChunk] int entityIndexInChunk, in PhysicsMass mass, ref PhysicsVelocity velocity)
    {
        var entityIndexInQuery = ChunkBaseEntityIndices[chunkIndexInQuery] + entityIndexInChunk;

        if (Densities[entityIndexInQuery] != 0)
        {
            var impulse = Forces[entityIndexInQuery] / Densities[entityIndexInQuery] / mass.InverseMass * DeltaTime;
            velocity.ApplyLinearImpulse(mass, impulse);
        }
    }
}