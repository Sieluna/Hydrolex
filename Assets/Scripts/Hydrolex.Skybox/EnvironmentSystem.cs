using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateBefore(typeof(HybridLightBakingDataSystem))]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class EnvironmentSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Environment, Celestium>().Build());
    }

    protected override void OnUpdate()
    {
        foreach (var (environment, celestial, entity) in SystemAPI
                     .Query<RefRO<Environment>, Celestium>()
                     .WithEntityAccess())
        {
            ref var lightTransform = ref SystemAPI.GetComponentRW<LocalTransform>(environment.ValueRO.LightTransform).ValueRW;
            ref var sunTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.SunTransform).ValueRW;
            ref var transform = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;

            var probe = EntityManager.GetComponentObject<ReflectionProbe>(environment.ValueRO.ReflectionProbeTransform);
            var flare = EntityManager.GetComponentObject<LensFlareComponentSRP>(environment.ValueRO.LightTransform);
            var light = EntityManager.GetComponentObject<Light>(environment.ValueRO.LightTransform);

            lightTransform.Rotation = GetDirectionalLightRotation(celestial, math.dot(-sunTransform.Forward(), transform.Up()));

            UpdateReflectionProbe(probe, environment.ValueRO, SystemAPI.Time.DeltaTime);

            flare.intensity = environment.ValueRO.FlareIntensity;

            light.intensity = environment.ValueRO.LightIntensity;
            light.color = environment.ValueRO.LightColor;

            RenderSettings.ambientIntensity = environment.ValueRO.AmbientIntensity;
            RenderSettings.ambientLight = environment.ValueRO.AmbientSkyColor;
            RenderSettings.ambientSkyColor = environment.ValueRO.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = environment.ValueRO.EquatorSkyColor;
            RenderSettings.ambientGroundColor =  environment.ValueRO.GroundSkyColor;
        }
    }

    private quaternion GetDirectionalLightRotation(in Celestium celestial, float sunElevation)
    {
        var lightDirection = sunElevation >= 0.0f ? celestial.SunLocalDirection : celestial.MoonLocalDirection;
        return quaternion.LookRotation(lightDirection, math.up());
    }

    private void UpdateReflectionProbe(ReflectionProbe probe, Environment environment, float deltaTime)
    {
#if UNITY_EDITOR
        if (probe is not null)
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = environment.RefreshMode;
            probe.timeSlicingMode = environment.TimeSlicingMode;
        }
#endif

        if (probe is null || environment.State != ReflectionProbeState.On) return;

        if (environment.RefreshMode == ReflectionProbeRefreshMode.EveryFrame)
        {
            probe.RenderProbe();
            //DynamicGI.UpdateEnvironment();
            return;
        }

        if (environment.RefreshMode != ReflectionProbeRefreshMode.ViaScripting) return;

        environment.TimeSinceLastProbeUpdate += deltaTime;

        if (environment.TimeSinceLastProbeUpdate >= environment.ProbeRefreshInterval)
        {
            probe.RenderProbe();
            //DynamicGI.UpdateEnvironment();
            environment.TimeSinceLastProbeUpdate = 0;
        }
    }
}