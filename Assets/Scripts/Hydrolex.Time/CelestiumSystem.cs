using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial struct CelestiumSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Celestium, Time>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (celestial, time, entity) in SystemAPI.Query<RefRW<Celestium>, Time>().WithEntityAccess())
        {
            ref var sunTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.ValueRW.SunTransform).ValueRW;
            ref var moonTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.ValueRW.MoonTransform).ValueRW;
            ref var transform = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;

            sunTransform.Rotation = GetSunDirection(celestial.ValueRO.Longitude, celestial.ValueRO.Latitude, time.Timeline, celestial.ValueRO.Utc);
            celestial.ValueRW.SunLocalDirection = transform.InverseTransformDirection(sunTransform.Forward());

            moonTransform.Rotation = GetMoonDirection(celestial.ValueRO.SunLocalDirection);
            celestial.ValueRW.MoonLocalDirection = transform.InverseTransformDirection(moonTransform.Forward());
        }
    }

    public static quaternion GetSunDirection(float longitude, float latitude, float time, float utc)
    {
        var earthTilt = quaternion.Euler(0.0f, math.radians(longitude), math.radians(latitude));
        var timeRotation = quaternion.Euler(((time + utc) * math.PI2 / 24.0f) - math.PIHALF, math.PI, 0.0f);

        return math.mul(earthTilt, timeRotation);
    }

    public static quaternion GetMoonDirection(float3 sunLocalDirection)
    {
        return math.abs(math.dot(-sunLocalDirection, math.up())) >= 1.0f - math.EPSILON
            ? new quaternion(new float3x3(float3.zero, float3.zero, -sunLocalDirection)) // collinear case
            : quaternion.LookRotation(-sunLocalDirection, math.up());
    }
}