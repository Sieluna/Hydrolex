using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public static class ParticlesUtilities
{
    private const int k_Concurrency = 32;

    public static JobHandle CreateParticles(AABB bounds, float4x4 localToWorld, float radius, EntityCommandBuffer ecb,
        Entity prefab, JobHandle dependencies)
    {
        var scale = new float3(math.length(localToWorld.c0.xyz),
            math.length(localToWorld.c1.xyz),
            math.length(localToWorld.c2.xyz));

        var scaledRadius = radius / scale;
        var numParticles = math.ceil(bounds.Size / (2 * scaledRadius));

        var totalParticles = (int)(numParticles.x * numParticles.y * numParticles.z);

        var job = new CreateParticlesAABBJob
        {
            BoundsMin = bounds.Min,
            NumberParticles = numParticles,
            LocalToWorldMatrix = localToWorld,
            ScaledRadius = scaledRadius,
            Prefab = prefab,
            CommandBuffer = ecb.AsParallelWriter()
        };

        return job.Schedule(totalParticles, k_Concurrency, dependencies);
    }
}


[BurstCompile]
public struct CreateParticlesAABBJob : IJobParallelFor
{
    public float3 BoundsMin;
    public float3 NumberParticles;
    public float4x4 LocalToWorldMatrix;
    public float3 ScaledRadius;
    public Entity Prefab;
    public EntityCommandBuffer.ParallelWriter CommandBuffer;

    private static int3 Get3Dimension(int linearIndex, int3 dimensions)
    {
        var z = linearIndex / (dimensions.x * dimensions.y);
        var y = (linearIndex / dimensions.x) % dimensions.y;
        var x = linearIndex % dimensions.x;

        return new int3(x, y, z);
    }

    public void Execute(int index)
    {
        var xyz = Get3Dimension(index, (int3)NumberParticles);

        var position = math.transform(LocalToWorldMatrix, xyz * 2 * ScaledRadius + BoundsMin + ScaledRadius);

        var entity = CommandBuffer.Instantiate(index, Prefab);

        CommandBuffer.SetComponent(index, entity, LocalTransform.FromPosition(position));
    }
}