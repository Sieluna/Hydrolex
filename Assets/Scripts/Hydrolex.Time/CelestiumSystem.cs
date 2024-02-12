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

            switch (celestial.ValueRO.SimulationType)
            {
                case CelestiumSimulation.Simple:
                    sunTransform.Rotation = GetChimericalSunDirection(celestial.ValueRO, time.Timeline);
                    celestial.ValueRW.SunLocalDirection = transform.InverseTransformDirection(sunTransform.Forward());

                    moonTransform.Rotation = GetChimericalMoonDirection(celestial.ValueRO.SunLocalDirection);
                    celestial.ValueRW.MoonLocalDirection = transform.InverseTransformDirection(moonTransform.Forward());

                    break;
                case CelestiumSimulation.Realistic:
                    // TODO: A better simulation should be created.
                    break;
            }
        }
    }

    public static quaternion GetChimericalSunDirection(in Celestium celestial, float time)
    {
        var earthTilt = quaternion.Euler(0.0f, math.radians(celestial.Longitude), math.radians(celestial.Latitude));
        var timeRotation = quaternion.Euler(((time + celestial.Utc) * math.PI2 / 24.0f) - math.PIHALF, math.PI, 0.0f);

        return math.mul(earthTilt, timeRotation);
    }

    public static quaternion GetChimericalMoonDirection(float3 sunLocalDirection)
    {
        return math.abs(math.dot(-sunLocalDirection, math.up())) >= 1.0f - math.EPSILON
            ? new quaternion(new float3x3(float3.zero, float3.zero, -sunLocalDirection)) // collinear case
            : quaternion.LookRotation(-sunLocalDirection, math.up());
    }
}