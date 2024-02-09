using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Skybox : IComponentData
{
    public Color RayleighColor;
    public Color MieColor;
    public float Rayleigh;
    public float Mie;
    public float Kr;
    public float Km;
    public float Scattering;
    public float SunIntensity;
    public float NightIntensity;
    public float Exposure;
    public BlobAssetReference<BlobGradient> RayleighGradientColor;
    public BlobAssetReference<BlobGradient> MieGradientColor;
    public BlobAssetReference<BlobCurve> RayleighCurve;
    public BlobAssetReference<BlobCurve> MieCurve;
    public BlobAssetReference<BlobCurve> KrCurve;
    public BlobAssetReference<BlobCurve> KmCurve;
    public BlobAssetReference<BlobCurve> ScatteringCurve;
    public BlobAssetReference<BlobCurve> SunIntensityCurve;
    public BlobAssetReference<BlobCurve> NightIntensityCurve;
    public BlobAssetReference<BlobCurve> ExposureCurve;

    public float StarfieldIntensity;
    public float MilkyWayIntensity;
    public float3 StarfieldColorBalance;
    public float3 StarfieldPosition;
    public BlobAssetReference<BlobCurve> StarfieldIntensityCurve;
    public BlobAssetReference<BlobCurve> MilkyWayIntensityCurve;
    public float MoonBrightRange;
    public Color MoonDiskColor;
    public Color MoonBrightColor;
    public BlobAssetReference<BlobGradient> MoonDiskGradientColor;
    public BlobAssetReference<BlobGradient> MoonBrightGradientColor;
    public BlobAssetReference<BlobCurve> MoonBrightRangeCurve;

    public Color CloudColor;
    public BlobAssetReference<BlobGradient> CloudGradientColor;
    public float CloudScattering;
    public BlobAssetReference<BlobCurve> CloudScatteringCurve;
    public float CloudExtinction;
    public BlobAssetReference<BlobCurve> CloudExtinctionCurve;
    public float CloudPower;
    public BlobAssetReference<BlobCurve> CloudPowerCurve;
    public float CloudIntensity;
    public BlobAssetReference<BlobCurve> CloudIntensityCurve;
    public float CloudRotationSpeed;
    public float CurrentCloudRotationSpeed;

    public float SunDiskSize;
    public float MoonDiskSize;
}