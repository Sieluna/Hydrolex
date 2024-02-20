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
    private static readonly int s_Rayleigh = Shader.PropertyToID("_Rayleigh");
    private static readonly int s_Mie = Shader.PropertyToID("_Mie");
    private static readonly int s_Scattering = Shader.PropertyToID("_Scattering");
    private static readonly int s_Luminance = Shader.PropertyToID("_Luminance");
    private static readonly int s_Exposure = Shader.PropertyToID("_Exposure");
    private static readonly int s_RayleighColor = Shader.PropertyToID("_RayleighColor");
    private static readonly int s_MieColor = Shader.PropertyToID("_MieColor");
    private static readonly int s_ScatteringColor = Shader.PropertyToID("_ScatteringColor");

    // Outer space
    private static readonly int s_SunTextureSize = Shader.PropertyToID("_SunTextureSize");
    private static readonly int s_SunTextureData = Shader.PropertyToID("_SunTextureData");
    private static readonly int s_MoonTextureSize = Shader.PropertyToID("_MoonTextureSize");
    private static readonly int s_MoonTextureData = Shader.PropertyToID("_MoonTextureData");
    private static readonly int s_StarsIntensity = Shader.PropertyToID("_StarsIntensity");
    private static readonly int s_MilkyWayIntensity = Shader.PropertyToID("_MilkyWayIntensity");
    private static readonly int s_StarFieldColor = Shader.PropertyToID("_StarFieldColor");
    private static readonly int s_StarFieldRotation = Shader.PropertyToID("_StarFieldRotationMatrix");

    // Clouds
    private static readonly int s_CloudData = Shader.PropertyToID("_CloudData");
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
            RenderSettings.skybox.SetVector(s_Rayleigh, new float4(ComputeRayleighCoefficient(skybox.ValueRO.Wavelength, skybox.ValueRO.MolecularDensity) * skybox.ValueRO.Rayleigh, skybox.ValueRO.Kr * 1000f));
            RenderSettings.skybox.SetVector(s_Mie, new float4(ComputeMieCoefficient(skybox.ValueRO.Wavelength) * skybox.ValueRO.Mie, skybox.ValueRO.Km * 1000f));
            RenderSettings.skybox.SetFloat(s_Scattering, skybox.ValueRO.Scattering * 60f);
            RenderSettings.skybox.SetFloat(s_Luminance, skybox.ValueRO.Luminance);
            RenderSettings.skybox.SetFloat(s_Exposure, skybox.ValueRO.Exposure);
            RenderSettings.skybox.SetColor(s_RayleighColor, skybox.ValueRO.RayleighColor);
            RenderSettings.skybox.SetColor(s_MieColor, skybox.ValueRO.MieColor);
            RenderSettings.skybox.SetColor(s_ScatteringColor, skybox.ValueRO.ScatteringColor);

            // Outer space
            RenderSettings.skybox.SetFloat(s_SunTextureSize, skybox.ValueRO.SunTextureSize);
            RenderSettings.skybox.SetVector(s_SunTextureData, new float4(GetRGB(skybox.ValueRO.SunTextureColor), skybox.ValueRO.SunTextureIntensity));
            RenderSettings.skybox.SetFloat(s_MoonTextureSize, skybox.ValueRO.MoonTextureSize);
            RenderSettings.skybox.SetVector(s_MoonTextureData, new float4(GetRGB(skybox.ValueRO.MoonTextureColor), skybox.ValueRO.MoonTextureIntensity));
            RenderSettings.skybox.SetFloat(s_StarsIntensity, skybox.ValueRO.StarsIntensity);
            RenderSettings.skybox.SetFloat(s_MilkyWayIntensity, skybox.ValueRO.MilkyWayIntensity);
            RenderSettings.skybox.SetColor(s_StarFieldColor, skybox.ValueRO.StarfieldColor);
            RenderSettings.skybox.SetMatrix(s_StarFieldRotation, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skybox.ValueRO.StarfieldRotation), Vector3.one).inverse);

            // Clouds
            skybox.ValueRW.CloudsPosition = ComputeCloudPosition(skybox.ValueRO.CloudsPosition, skybox.ValueRO.CloudsDirection, skybox.ValueRO.CloudsSpeed, SystemAPI.Time.DeltaTime);
            RenderSettings.skybox.SetVector(s_CloudData, new float4(skybox.ValueRO.CloudsPosition, skybox.ValueRO.CloudsAltitude, Mathf.Lerp(25.0f, 0.0f, skybox.ValueRO.CloudsDensity)));
            RenderSettings.skybox.SetVector(s_CloudColor1, skybox.ValueRO.CloudsColor1);
            RenderSettings.skybox.SetVector(s_CloudColor2, skybox.ValueRO.CloudsColor2);
        }
    }

    private static float3 GetRGB(Color color) => new(color.r, color.g, color.b);

    /// <summary>
    /// Total scattering coefficients for Rayleigh scattering for molecules.
    /// `"A Practical Analytic Model for Daylight" Preetham et al.`
    /// </summary>
    /// <param name="wavelength"></param>
    /// <param name="molecularDensity"></param>
    /// <returns></returns>
    public static float3 ComputeRayleighCoefficient(float3 wavelength, float molecularDensity = 2.545f)
    {
        var lambda = wavelength * 1e-9f;
        var N = molecularDensity * 1e25f;

        const float n = 1.0003f; // Refractive index of air(1.0003 in the visible spectrum)
        const float pn = 0.035f; // Depolarization factor(0.035 standard for air)

        return 8.0f * math.pow(math.PI, 3.0f) * math.pow(n * n - 1.0f, 2.0f) / (3.0f * N * math.pow(lambda, 4.0f)) *
               ((6.0f + 3.0f * pn) / (6.0f - 7.0f * pn));
    }

    /// <summary>
    /// Total scattering coefficients for Mie scattering for haze.
    /// `"A Practical Analytic Model for Daylight" Preetham et al.`
    /// </summary>
    /// <param name="wavelength"></param>
    /// <param name="turbidity"></param>
    /// <returns></returns>
    public static float3 ComputeMieCoefficient(float3 wavelength, float turbidity = 5.0f)
    {
        var lambda = wavelength * 1e-9f;

        var c = (0.6544f * turbidity - 0.6510f) * 1e-16f; // concentration factor
        // A poly - 3 curve fit for K only between [380nm - 780nm]
        var K = 5.343428e17f * math.pow(lambda, 3.0f) - 1.167102e12f * math.pow(lambda, 2.0f) + 8.895071e5f * lambda + 4.526041e-1f;
        var v = 4.0f; // Junge's exponent

        return 0.434f * c * math.PI * math.pow((2.0f * math.PI) / lambda, v - 2.0f) * K * 1e-4f;
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
        var dirRadians = math.radians(math.lerp(0f, 360f, cloudsDirection)) * 0.01745329f;
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