using Unity.Burst;
using Unity.Entities;

[UpdateBefore(typeof(SkyboxSystem))]
public partial struct WeatherSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Environment, Skybox, Weather, Time>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (environment, skybox, weather, time) in SystemAPI
                     .Query<RefRW<Environment>, RefRW<Skybox>, RefRW<Weather>, RefRO<Time>>())
        {
            var (curveTime, gradientTime) = (time.ValueRO.Timeline, time.ValueRO.Timeline / 24.0f);

            ref var currentWeather = ref weather.ValueRO.WeatherPool.Value.Weathers[weather.ValueRO.CurrentWeather];

            EvaluateScattering(ref skybox.ValueRW, ref currentWeather, curveTime, gradientTime);

            EvaluateCelestium(ref skybox.ValueRW, ref currentWeather, curveTime);

            EvaluateEnvironment(ref environment.ValueRW, ref currentWeather, curveTime, gradientTime);

            EvaluateClouds(ref skybox.ValueRW, ref currentWeather, gradientTime);
        }
    }

    private void EvaluateScattering(ref Skybox skybox, ref BlobWeather weather, float curveTime, float gradientTime)
    {
        skybox.MolecularDensity = weather.MolecularDensity * 5.09f;
        skybox.Rayleigh = weather.RayleighCurve.Evaluate(curveTime) * 10.0f;
        skybox.Mie = weather.MieCurve.Evaluate(curveTime) * 10.0f;
        skybox.RayleighColor = weather.RayleighGradientColor.Evaluate(gradientTime);
        skybox.MieColor = weather.MieGradientColor.Evaluate(gradientTime);
    }

    private void EvaluateCelestium(ref Skybox skybox, ref BlobWeather weather, float curveTime)
    {
        skybox.SunTextureIntensity = weather.SunTextureIntensity;
        skybox.MoonTextureIntensity = weather.MoonTextureIntensity;
        skybox.StarsIntensity = weather.StarsIntensityCurve.Evaluate(curveTime);
        skybox.MilkyWayIntensity = weather.MilkyWayIntensityCurve.Evaluate(curveTime);
    }

    private void EvaluateEnvironment(ref Environment environment, ref BlobWeather weather, float curveTime, float gradientTime)
    {
        environment.LightIntensity = weather.LightIntensityCurve.Evaluate(curveTime);
        environment.LightColor = weather.LightGradientColor.Evaluate(gradientTime);
        environment.FlareIntensity = weather.FlareIntensityCurve.Evaluate(curveTime);
        environment.AmbientIntensity = weather.AmbientIntensityCurve.Evaluate(curveTime);
        environment.AmbientSkyColor = weather.AmbientSkyGradientColor.Evaluate(gradientTime);
        environment.EquatorSkyColor = weather.EquatorSkyGradientColor.Evaluate(gradientTime);
        environment.GroundSkyColor = weather.GroundSkyGradientColor.Evaluate(gradientTime);
    }

    private void EvaluateClouds(ref Skybox skybox, ref BlobWeather weather, float gradientTime)
    {
        skybox.CloudsAltitude = weather.CloudsAltitude * 15.0f;
        skybox.CloudsDirection = weather.CloudsDirection;
        skybox.CloudsSpeed = weather.CloudsSpeed;
        skybox.CloudsDensity = weather.CloudsDensity;
        skybox.CloudsColor1 = weather.CloudsGradientColor1.Evaluate(gradientTime);
        skybox.CloudsColor2 = weather.CloudsGradientColor2.Evaluate(gradientTime);
    }
}