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
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Skybox, Environment, Time, Celestium>().Build());
    }

    protected override void OnUpdate()
    {
        foreach (var (environment, time, celestial, entity) in SystemAPI
                     .Query<RefRW<Environment>, Time, Celestium>()
                     .WithEntityAccess())
        {
            var (curveTime, gradientTime) = (time.Timeline, time.Timeline / 24.0f);

            ref var lightTransform = ref SystemAPI.GetComponentRW<LocalTransform>(environment.ValueRO.LightTransform).ValueRW;
            ref var sunTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.SunTransform).ValueRW;
            ref var transform = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;

            var flare = EntityManager.GetComponentObject<LensFlareComponentSRP>(environment.ValueRO.LightTransform);
            var light = EntityManager.GetComponentObject<Light>(environment.ValueRO.LightTransform);

            EvaluateEnvironment(ref environment.ValueRW, curveTime, gradientTime);

            lightTransform.Rotation = GetDirectionalLightRotation(celestial, math.dot(-sunTransform.Forward(), transform.Up()));

            flare.intensity = environment.ValueRO.FlareIntensity;

            light.intensity = environment.ValueRO.LightIntensity;
            light.color = environment.ValueRW.LightColor;

            RenderSettings.sun ??= light;
            RenderSettings.ambientIntensity = environment.ValueRO.AmbientIntensity;
            RenderSettings.ambientSkyColor = environment.ValueRO.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = environment.ValueRO.EquatorSkyColor;
            RenderSettings.ambientGroundColor =  environment.ValueRO.GroundSkyColor;
        }
    }
    private void EvaluateEnvironment(ref Environment environment, float curveTime, float gradientTime)
    {
        environment.LightIntensity = environment.LightIntensityCurve.Value.Evaluate(curveTime);
        environment.LightColor = environment.LightGradientColor.Value.Evaluate(gradientTime);
        environment.FlareIntensity = environment.FlareIntensityCurve.Value.Evaluate(curveTime);
        environment.AmbientIntensity = environment.AmbientIntensityCurve.Value.Evaluate(curveTime);
        environment.AmbientSkyColor = environment.AmbientSkyGradientColor.Value.Evaluate(gradientTime);
        environment.EquatorSkyColor = environment.EquatorSkyGradientColor.Value.Evaluate(gradientTime);
        environment.GroundSkyColor = environment.GroundSkyGradientColor.Value.Evaluate(gradientTime);
    }

    private quaternion GetDirectionalLightRotation(in Celestium celestial, float sunElevation)
    {
        var lightDirection = sunElevation >= 0.0f ? celestial.SunLocalDirection : celestial.MoonLocalDirection;
        return quaternion.LookRotation(lightDirection, math.up());
    }
}