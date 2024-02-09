using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial class SkyboxSystem : SystemBase
{
    // Scattering
    private static readonly int s_Br = Shader.PropertyToID("_Br");
    private static readonly int s_Bm = Shader.PropertyToID("_Bm");
    private static readonly int s_Kr = Shader.PropertyToID("_Kr");
    private static readonly int s_Km = Shader.PropertyToID("_Km");
    private static readonly int s_Scattering = Shader.PropertyToID("_Scattering");
    private static readonly int s_SunIntensity = Shader.PropertyToID("_SunIntensity");
    private static readonly int s_NightIntensity = Shader.PropertyToID("_NightIntensity");
    private static readonly int s_Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int s_RayleighColor = Shader.PropertyToID("_RayleighColor");
    private static readonly int s_MieColor = Shader.PropertyToID("_MieColor");
    private static readonly int s_MieG = Shader.PropertyToID("_MieG");

    // Night sky
    private static readonly int s_MoonDiskColor = Shader.PropertyToID("_MoonDiskColor");
    private static readonly int s_MoonBrightColor = Shader.PropertyToID("_MoonBrightColor");
    private static readonly int s_MoonBrightRange = Shader.PropertyToID("_MoonBrightRange");
    private static readonly int s_StarfieldIntensity = Shader.PropertyToID("_StarfieldIntensity");
    private static readonly int s_MilkyWayIntensity = Shader.PropertyToID("_MilkyWayIntensity");
    private static readonly int s_StarfieldColorBalance = Shader.PropertyToID("_StarfieldColorBalance");
        
    // Clouds
    private static readonly int s_CloudColor = Shader.PropertyToID("_CloudColor");
    private static readonly int s_CloudScattering = Shader.PropertyToID("_CloudScattering");
    private static readonly int s_CloudExtinction = Shader.PropertyToID("_CloudExtinction");
    private static readonly int s_CloudPower = Shader.PropertyToID("_CloudPower");
    private static readonly int s_CloudIntensity = Shader.PropertyToID("_CloudIntensity");
    private static readonly int s_CloudRotationSpeed = Shader.PropertyToID("_CloudRotationSpeed");

    // Directions
    private static readonly int s_SunDirection = Shader.PropertyToID("_SunDirection");
    private static readonly int s_MoonDirection = Shader.PropertyToID("_MoonDirection");

    // Matrix
    private static readonly int s_SkyUpDirectionMatrix = Shader.PropertyToID("_SkyUpDirectionMatrix");
    private static readonly int s_SunMatrix = Shader.PropertyToID("_SunMatrix");
    private static readonly int s_MoonMatrix = Shader.PropertyToID("_MoonMatrix");
    private static readonly int s_StarfieldMatrix = Shader.PropertyToID("_StarfieldMatrix");

    private static readonly int s_SunDiskSize = Shader.PropertyToID("_SunDiskSize");
    private static readonly int s_MoonDiskSize = Shader.PropertyToID("_MoonDiskSize");

    private static readonly float3 m_Br = new float3(0.00519673f, 0.0121427f, 0.0296453f);
    private static readonly float3 m_Bm = new float3(0.005721017f, 0.004451339f, 0.003146905f);

    protected override void OnCreate()
    {
        RequireForUpdate(SystemAPI.QueryBuilder()
            .WithAll<Skybox, Time, Celestium>()
            .Build());
    }

    protected override void OnUpdate()
    {
        foreach (var (skybox, time, celestial, entity) in SystemAPI
                     .Query<RefRW<Skybox>, Time, Celestium>()
                     .WithEntityAccess())
        {
            var (curveTime, gradientTime) = (time.Timeline, time.Timeline / 24.0f);

            ref var transform = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;
            ref var sunTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.SunTransform).ValueRW;
            ref var moonTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.MoonTransform).ValueRW;

            EvaluateScattering(ref skybox.ValueRW, curveTime, gradientTime);

            EvaluateNightSky(ref skybox.ValueRW, curveTime, gradientTime);

            EvaluateClouds(ref skybox.ValueRW, curveTime, gradientTime, SystemAPI.Time.DeltaTime);

            // Directions
            Shader.SetGlobalVector(s_SunDirection, new float4(-celestial.SunLocalDirection, 0));
            Shader.SetGlobalVector(s_MoonDirection, new float4(-celestial.MoonLocalDirection, 0));

            // Matrix
            Shader.SetGlobalMatrix(s_SkyUpDirectionMatrix, transform.ToMatrix());
            Shader.SetGlobalMatrix(s_SunMatrix, sunTransform.ToMatrix());
            Shader.SetGlobalMatrix(s_MoonMatrix, moonTransform.ToMatrix());
            Shader.SetGlobalMatrix(s_StarfieldMatrix, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skybox.ValueRO.StarfieldPosition), Vector3.one).inverse);

            Shader.SetGlobalFloat(s_SunDiskSize, Mathf.Lerp(5.0f, 1.0f, skybox.ValueRO.SunDiskSize));
            Shader.SetGlobalFloat(s_MoonDiskSize, Mathf.Lerp(20.0f, 1.0f, skybox.ValueRO.MoonDiskSize));
        }
    }

    private void EvaluateScattering(ref Skybox skybox, float curveTime, float gradientTime)
    {
        skybox.Rayleigh = skybox.RayleighCurve.Value.Evaluate(curveTime);
        skybox.Mie = skybox.MieCurve.Value.Evaluate(curveTime);
        skybox.Kr = skybox.KrCurve.Value.Evaluate(curveTime);
        skybox.Km = skybox.KmCurve.Value.Evaluate(curveTime);
        skybox.Scattering = skybox.ScatteringCurve.Value.Evaluate(curveTime);
        skybox.SunIntensity = skybox.SunIntensityCurve.Value.Evaluate(curveTime);
        skybox.NightIntensity = skybox.NightIntensityCurve.Value.Evaluate(curveTime);
        skybox.Exposure = skybox.ExposureCurve.Value.Evaluate(curveTime);
        skybox.RayleighColor = skybox.RayleighGradientColor.Value.Evaluate(gradientTime);
        skybox.MieColor = skybox.MieGradientColor.Value.Evaluate(gradientTime);

        Shader.SetGlobalVector(s_Br, new float4(m_Br * skybox.Rayleigh, 0));
        Shader.SetGlobalVector(s_Bm, new float4(m_Bm * skybox.Mie, 0));
        Shader.SetGlobalFloat(s_Kr, skybox.Kr);
        Shader.SetGlobalFloat(s_Km, skybox.Km);
        Shader.SetGlobalFloat(s_Scattering, skybox.Scattering);
        Shader.SetGlobalFloat(s_SunIntensity, skybox.SunIntensity);
        Shader.SetGlobalFloat(s_NightIntensity, skybox.NightIntensity);
        Shader.SetGlobalFloat(s_Exposure, skybox.Exposure);
        Shader.SetGlobalColor(s_RayleighColor, skybox.RayleighColor);
        Shader.SetGlobalColor(s_MieColor, skybox.MieColor);
    }

    private void EvaluateNightSky(ref Skybox skybox, float curveTime, float gradientTime)
    {
        skybox.MoonDiskColor = skybox.MoonDiskGradientColor.Value.Evaluate(gradientTime);
        skybox.MoonBrightColor = skybox.MoonBrightGradientColor.Value.Evaluate(gradientTime);
        skybox.MoonBrightRange = skybox.MoonBrightRangeCurve.Value.Evaluate(curveTime);
        skybox.StarfieldIntensity = skybox.StarfieldIntensityCurve.Value.Evaluate(curveTime);
        skybox.MilkyWayIntensity = skybox.MilkyWayIntensityCurve.Value.Evaluate(curveTime);

        Shader.SetGlobalColor(s_MoonDiskColor, skybox.MoonDiskColor);
        Shader.SetGlobalColor(s_MoonBrightColor, skybox.MoonBrightColor);
        Shader.SetGlobalFloat(s_MoonBrightRange, Mathf.Lerp(150.0f, 5.0f, skybox.MoonBrightRange));
        Shader.SetGlobalFloat(s_StarfieldIntensity, skybox.StarfieldIntensity);
        Shader.SetGlobalFloat(s_MilkyWayIntensity, skybox.MilkyWayIntensity);
        Shader.SetGlobalVector(s_StarfieldColorBalance, new float4(skybox.StarfieldColorBalance, 0));
    }

    private void EvaluateClouds(ref Skybox skybox, float curveTime, float gradientTime, float deltaTime)
    {
        skybox.CloudColor = skybox.CloudGradientColor.Value.Evaluate(gradientTime);
        skybox.CloudScattering = skybox.CloudScatteringCurve.Value.Evaluate(curveTime);
        skybox.CloudExtinction = skybox.CloudExtinctionCurve.Value.Evaluate(curveTime);
        skybox.CloudPower = skybox.CloudPowerCurve.Value.Evaluate(curveTime);
        skybox.CloudIntensity = skybox.CloudIntensityCurve.Value.Evaluate(curveTime);
        if (skybox.CloudRotationSpeed != 0.0f)
        {
            skybox.CurrentCloudRotationSpeed += skybox.CloudRotationSpeed * deltaTime;
            if (skybox.CurrentCloudRotationSpeed >= 1.0f) skybox.CurrentCloudRotationSpeed -= 1.0f;
        }

        Shader.SetGlobalColor(s_CloudColor, skybox.CloudColor);
        Shader.SetGlobalFloat(s_CloudScattering, skybox.CloudScattering);
        Shader.SetGlobalFloat(s_CloudExtinction, skybox.CloudExtinction);
        Shader.SetGlobalFloat(s_CloudPower, skybox.CloudPower);
        Shader.SetGlobalFloat(s_CloudIntensity, skybox.CloudIntensity);
        Shader.SetGlobalFloat(s_CloudRotationSpeed, skybox.CurrentCloudRotationSpeed);
    }

    /// <summary>
    /// Total rayleigh computation.
    /// </summary>
    /// <param name="wavelength"></param>
    /// <param name="molecularDensity"></param>
    /// <returns></returns>
    private float3 ComputeRayleigh(float3 wavelength, float molecularDensity)
    {
        var lambda = wavelength * 1e-9f;
        var n = 1.0003f; // Refractive index of air
        var pn = 0.035f; // Depolarization factor for standard air.
        var n2 = n * n;
        var N = molecularDensity * 1E25f;
        var temp = (8.0f * math.PI * math.PI * math.PI * ((n2 - 1.0f) * (n2 - 1.0f))) / (3.0f * N) * ((6.0f + 3.0f * pn) / (6.0f - 7.0f * pn));

        return temp / math.pow(lambda, 4.0f);
    }

    /// <summary>
    /// Total mie computation.
    /// </summary>
    /// <param name="wavelength"></param>
    /// <returns></returns>
    private float3 ComputeMie(float3 wavelength)
    {
        var c = (0.6544f * 5.0f - 0.6510f) * 10f * 1e-9f;
        var k = new float3(686.0f, 678.0f, 682.0f);

        return 434.0f * c * math.PI * math.pow((4.0f * math.PI) / wavelength, 2.0f) * k;
    }
}