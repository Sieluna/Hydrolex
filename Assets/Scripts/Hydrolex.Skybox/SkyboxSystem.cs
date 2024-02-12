using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial class SkyboxSystem : SystemBase
{
    // Directions
    private static readonly int s_SunDirection = Shader.PropertyToID("_SunDirection");
    private static readonly int s_MoonDirection = Shader.PropertyToID("_MoonDirection");
    private static readonly int s_SunMatrix = Shader.PropertyToID("_SunMatrix");
    private static readonly int s_MoonMatrix = Shader.PropertyToID("_MoonMatrix");
    private static readonly int s_UpDirectionMatrix = Shader.PropertyToID("_UpDirectionMatrix");
    private static readonly int s_StarfieldMatrix = Shader.PropertyToID("_StarfieldMatrix");

    // Scattering
    private static readonly int s_Kr = Shader.PropertyToID("_Kr");
    private static readonly int s_Km = Shader.PropertyToID("_Km");
    private static readonly int s_Rayleigh = Shader.PropertyToID("_Rayleigh");
    private static readonly int s_Mie = Shader.PropertyToID("_Mie");
    private static readonly int s_MieDistance = Shader.PropertyToID("_MieDepth");
    private static readonly int s_Scattering = Shader.PropertyToID("_Scattering");
    private static readonly int s_Luminance = Shader.PropertyToID("_Luminance");
    private static readonly int s_Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int s_RayleighColor = Shader.PropertyToID("_RayleighColor");
    private static readonly int s_MieColor = Shader.PropertyToID("_MieColor");
    private static readonly int s_ScatteringColor = Shader.PropertyToID("_ScatteringColor");

    // Outer space
    private static readonly int s_SunTextureSize = Shader.PropertyToID("_SunTextureSize");
    private static readonly int s_SunTextureIntensity = Shader.PropertyToID("_SunTextureIntensity");
    private static readonly int s_SunTextureColor = Shader.PropertyToID("_SunTextureColor");
    private static readonly int s_MoonTextureSize = Shader.PropertyToID("_MoonTextureSize");
    private static readonly int s_MoonTextureIntensity = Shader.PropertyToID("_MoonTextureIntensity");
    private static readonly int s_MoonTextureColor = Shader.PropertyToID("_MoonTextureColor");
    private static readonly int s_StarsIntensity = Shader.PropertyToID("_StarsIntensity");
    private static readonly int s_MilkyWayIntensity = Shader.PropertyToID("_MilkyWayIntensity");
    private static readonly int s_StarFieldColor = Shader.PropertyToID("_StarFieldColor");
    private static readonly int s_StarFieldRotation = Shader.PropertyToID("_StarFieldRotationMatrix");

    // Clouds
    private static readonly int s_CloudAltitude = Shader.PropertyToID("_CloudAltitude");
    private static readonly int s_CloudDirection = Shader.PropertyToID("_CloudDirection");
    private static readonly int s_CloudDensity = Shader.PropertyToID("_CloudDensity");
    private static readonly int s_CloudColor1 = Shader.PropertyToID("_CloudColor1");
    private static readonly int s_CloudColor2 = Shader.PropertyToID("_CloudColor2");

    protected override void OnCreate()
    {
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Skybox, Celestium>().Build());
    }

    protected override void OnUpdate()
    {
        foreach (var (skybox, celestial, entity) in SystemAPI.Query<RefRW<Skybox>, Celestium>().WithEntityAccess())
        {
            ref var sunTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.SunTransform).ValueRW;
            ref var moonTransform = ref SystemAPI.GetComponentRW<LocalTransform>(celestial.MoonTransform).ValueRW;
            ref var transform = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;

            // Directions
            Shader.SetGlobalVector(s_SunDirection, new float4(-celestial.SunLocalDirection, 0));
            Shader.SetGlobalVector(s_MoonDirection, new float4(-celestial.MoonLocalDirection, 0));
            Shader.SetGlobalMatrix(s_SunMatrix, sunTransform.ToMatrix());
            Shader.SetGlobalMatrix(s_MoonMatrix, moonTransform.ToMatrix());
            Shader.SetGlobalMatrix(s_UpDirectionMatrix, transform.ToMatrix());
            Shader.SetGlobalMatrix(s_StarfieldMatrix, sunTransform.ToMatrix());

            // Scattering
            Shader.SetGlobalFloat(s_Kr, skybox.ValueRO.Kr * 1000f);
            Shader.SetGlobalFloat(s_Km, skybox.ValueRO.Km * 1000f);
            Shader.SetGlobalVector(s_Rayleigh, new float4(ComputeRayleigh(skybox.ValueRO.Wavelength, skybox.ValueRO.MolecularDensity) * skybox.ValueRO.Rayleigh, 0));
            Shader.SetGlobalVector(s_Mie, new float4(ComputeMie(skybox.ValueRO.Wavelength) * skybox.ValueRO.Mie, 0));
            Shader.SetGlobalFloat(s_MieDistance, skybox.ValueRO.MieDistance);
            Shader.SetGlobalFloat(s_Scattering, skybox.ValueRO.Scattering * 60f);
            Shader.SetGlobalFloat(s_Luminance, skybox.ValueRO.Luminance);
            Shader.SetGlobalFloat(s_Exposure, skybox.ValueRO.Exposure);
            Shader.SetGlobalColor(s_RayleighColor, skybox.ValueRO.RayleighColor);
            Shader.SetGlobalColor(s_MieColor, skybox.ValueRO.MieColor);
            Shader.SetGlobalColor(s_ScatteringColor, skybox.ValueRO.ScatteringColor);

            // Outer space
            Shader.SetGlobalFloat(s_SunTextureSize, skybox.ValueRO.SunTextureSize);
            Shader.SetGlobalFloat(s_SunTextureIntensity, skybox.ValueRO.SunTextureIntensity);
            Shader.SetGlobalColor(s_SunTextureColor, skybox.ValueRO.SunTextureColor);
            Shader.SetGlobalFloat(s_MoonTextureSize, skybox.ValueRO.MoonTextureSize);
            Shader.SetGlobalFloat(s_MoonTextureIntensity, skybox.ValueRO.MoonTextureIntensity);
            Shader.SetGlobalColor(s_MoonTextureColor, skybox.ValueRO.MoonTextureColor);
            Shader.SetGlobalFloat(s_StarsIntensity, skybox.ValueRO.StarsIntensity);
            Shader.SetGlobalFloat(s_MilkyWayIntensity, skybox.ValueRO.MilkyWayIntensity);
            Shader.SetGlobalColor(s_StarFieldColor, skybox.ValueRO.StarfieldColor);
            Shader.SetGlobalMatrix(s_StarFieldRotation, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skybox.ValueRO.StarfieldRotation), Vector3.one).inverse);

            // Clouds
            skybox.ValueRW.CloudsPosition = ComputeCloudPosition(skybox.ValueRO.CloudsPosition, skybox.ValueRO.CloudsDirection, skybox.ValueRO.CloudsSpeed, SystemAPI.Time.DeltaTime);
            Shader.SetGlobalFloat(s_CloudAltitude, skybox.ValueRO.CloudsAltitude);
            Shader.SetGlobalVector(s_CloudDirection, new float4(skybox.ValueRO.CloudsPosition, 0, 0));
            Shader.SetGlobalFloat(s_CloudDensity, Mathf.Lerp(25.0f, 0.0f, skybox.ValueRO.CloudsDensity));
            Shader.SetGlobalVector(s_CloudColor1, skybox.ValueRO.CloudsColor1);
            Shader.SetGlobalVector(s_CloudColor2, skybox.ValueRO.CloudsColor2);
        }
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

    /// <summary>
    /// Returns the cloud uv position based on the direction and speed.
    /// </summary>
    /// <param name="cloudsPosition"></param>
    /// <param name="cloudsDirection"></param>
    /// <param name="cloudsSpeed"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    private float2 ComputeCloudPosition(float2 cloudsPosition, float cloudsDirection, float cloudsSpeed, float deltaTime)
    {
        var dirRadians = math.radians(math.lerp(0f, 360f, cloudsDirection));
        var windDirection = new float2(math.sin(dirRadians), math.cos(dirRadians));
        var windSpeed = cloudsSpeed * 0.05f * deltaTime;

        return math.frac(cloudsPosition + windSpeed * windDirection);
    }
}