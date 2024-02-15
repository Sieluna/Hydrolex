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
            RenderSettings.skybox.SetVector(s_SunDirection, new float4(-celestial.SunLocalDirection, 0));
            RenderSettings.skybox.SetVector(s_MoonDirection, new float4(-celestial.MoonLocalDirection, 0));
            RenderSettings.skybox.SetMatrix(s_SunMatrix, ComputeWorldToLocalMatrix(sunTransform));
            RenderSettings.skybox.SetMatrix(s_MoonMatrix, ComputeWorldToLocalMatrix(moonTransform));
            RenderSettings.skybox.SetMatrix(s_UpDirectionMatrix, ComputeWorldToLocalMatrix(transform));
            RenderSettings.skybox.SetMatrix(s_StarfieldMatrix, ComputeWorldToLocalMatrix(sunTransform));

            // Scattering
            RenderSettings.skybox.SetFloat(s_Kr, skybox.ValueRO.Kr * 1000f);
            RenderSettings.skybox.SetFloat(s_Km, skybox.ValueRO.Km * 1000f);
            RenderSettings.skybox.SetVector(s_Rayleigh, new float4(ComputeRayleigh(skybox.ValueRO.Wavelength, skybox.ValueRO.MolecularDensity) * skybox.ValueRO.Rayleigh, 0));
            RenderSettings.skybox.SetVector(s_Mie, new float4(ComputeMie(skybox.ValueRO.Wavelength) * skybox.ValueRO.Mie, 0));
            RenderSettings.skybox.SetFloat(s_MieDistance, skybox.ValueRO.MieDistance);
            RenderSettings.skybox.SetFloat(s_Scattering, skybox.ValueRO.Scattering * 60f);
            RenderSettings.skybox.SetFloat(s_Luminance, skybox.ValueRO.Luminance);
            RenderSettings.skybox.SetFloat(s_Exposure, skybox.ValueRO.Exposure);
            RenderSettings.skybox.SetColor(s_RayleighColor, skybox.ValueRO.RayleighColor);
            RenderSettings.skybox.SetColor(s_MieColor, skybox.ValueRO.MieColor);
            RenderSettings.skybox.SetColor(s_ScatteringColor, skybox.ValueRO.ScatteringColor);

            // Outer space
            RenderSettings.skybox.SetFloat(s_SunTextureSize, skybox.ValueRO.SunTextureSize);
            RenderSettings.skybox.SetFloat(s_SunTextureIntensity, skybox.ValueRO.SunTextureIntensity);
            RenderSettings.skybox.SetColor(s_SunTextureColor, skybox.ValueRO.SunTextureColor);
            RenderSettings.skybox.SetFloat(s_MoonTextureSize, skybox.ValueRO.MoonTextureSize);
            RenderSettings.skybox.SetFloat(s_MoonTextureIntensity, skybox.ValueRO.MoonTextureIntensity);
            RenderSettings.skybox.SetColor(s_MoonTextureColor, skybox.ValueRO.MoonTextureColor);
            RenderSettings.skybox.SetFloat(s_StarsIntensity, skybox.ValueRO.StarsIntensity);
            RenderSettings.skybox.SetFloat(s_MilkyWayIntensity, skybox.ValueRO.MilkyWayIntensity);
            RenderSettings.skybox.SetColor(s_StarFieldColor, skybox.ValueRO.StarfieldColor);
            RenderSettings.skybox.SetMatrix(s_StarFieldRotation, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skybox.ValueRO.StarfieldRotation), Vector3.one).inverse);

            // Clouds
            skybox.ValueRW.CloudsPosition = ComputeCloudPosition(skybox.ValueRO.CloudsPosition, skybox.ValueRO.CloudsDirection, skybox.ValueRO.CloudsSpeed, SystemAPI.Time.DeltaTime);
            RenderSettings.skybox.SetFloat(s_CloudAltitude, skybox.ValueRO.CloudsAltitude);
            RenderSettings.skybox.SetVector(s_CloudDirection, new float4(skybox.ValueRO.CloudsPosition, 0, 0));
            RenderSettings.skybox.SetFloat(s_CloudDensity, Mathf.Lerp(25.0f, 0.0f, skybox.ValueRO.CloudsDensity));
            RenderSettings.skybox.SetVector(s_CloudColor1, skybox.ValueRO.CloudsColor1);
            RenderSettings.skybox.SetVector(s_CloudColor2, skybox.ValueRO.CloudsColor2);
        }
    }

    /// <summary>
    /// Total rayleigh computation.
    /// </summary>
    /// <param name="wavelength"></param>
    /// <param name="molecularDensity"></param>
    /// <returns></returns>
    public static float3 ComputeRayleigh(float3 wavelength, float molecularDensity)
    {
        var lambda = wavelength * 1e-9f;
        const float n = 1.0003f; // Refractive index of air
        const float pn = 0.035f; // Depolarization factor for standard air.
        const float n2 = n * n;
        var N = molecularDensity * 1E25f;
        var temp = (8.0f * math.PI * math.PI * math.PI * ((n2 - 1.0f) * (n2 - 1.0f))) / (3.0f * N) * ((6.0f + 3.0f * pn) / (6.0f - 7.0f * pn));

        return temp / math.pow(lambda, 4.0f);
    }

    /// <summary>
    /// Total mie computation.
    /// </summary>
    /// <param name="wavelength"></param>
    /// <returns></returns>
    public static float3 ComputeMie(float3 wavelength)
    {
        const float c = (0.6544f * 5.0f - 0.6510f) * 10f * 1e-9f;
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
    public static float2 ComputeCloudPosition(float2 cloudsPosition, float cloudsDirection, float cloudsSpeed, float deltaTime)
    {
        var dirRadians = math.radians(math.lerp(0f, 360f, cloudsDirection));
        var windDirection = new float2(math.sin(dirRadians), math.cos(dirRadians));
        var windSpeed = cloudsSpeed * 0.05f * deltaTime;

        return math.frac(cloudsPosition + windSpeed * windDirection);
    }

    /// <summary>
    /// LocalTransform World to Local.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public static float4x4 ComputeWorldToLocalMatrix(LocalTransform transform)
    {
        var inverseScale = 1.0f / transform.Scale;
        var inverseRotation = math.inverse(transform.Rotation);
        var inversePosition = -transform.Position;

        return float4x4.TRS(inversePosition, inverseRotation, inverseScale);
    }
}