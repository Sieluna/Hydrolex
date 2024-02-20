using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SkyboxAuthoring : MonoBehaviour
{
    [Header("Scattering")]
    public float MolecularDensity = 2.545f;
    public float3 Wavelength = new(680, 550, 450);
    public float Kr = 8.4f;
    public float Km = 1.2f;
    public float Rayleigh = 1.5f;
    public float Mie = 1.24f;
    public float Scattering = 0.25f;
    public float Luminance = 1.0f;
    public float Exposure = 2.0f;
    public Color RayleighColor = new(0.77f, 0.9f, 1f);
    public Color MieColor = new(0.96f, 0.72f, 0.32f);
    public Color ScatteringColor = Color.white;

    [Header("Celestium")]
    public float SunTextureSize = 1.5f;
    public float SunTextureIntensity = 1.0f;
    public Color SunTextureColor = Color.white;
    public float MoonTextureSize = 5.0f;
    public float MoonTextureIntensity = 1.0f;
    public Color MoonTextureColor = Color.white;
    public float StarsIntensity = 0.0f;
    public float MilkyWayIntensity = 0.0f;
    public Color StarfieldColor = Color.white;
    public float3 StarfieldRotation = float3.zero;

    [Header("Clouds")]
    public float CloudsAltitude = 7.5f;
    public float CloudsDirection = 0.0f;
    public float CloudsSpeed = 0.1f;
    public float CloudsDensity = 0.75f;
    public Color CloudsColor1 = Color.white;
    public Color CloudsColor2 = Color.white;

    private class SkyboxBaker : Baker<SkyboxAuthoring>
    {
        public override void Bake(SkyboxAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Skybox
            {
                MolecularDensity = authoring.MolecularDensity,
                Wavelength = authoring.Wavelength,
                Kr = authoring.Kr,
                Km = authoring.Km,
                Rayleigh = authoring.Rayleigh,
                Mie = authoring.Mie,
                Scattering = authoring.Scattering,
                Luminance = authoring.Luminance,
                Exposure = authoring.Exposure,
                RayleighColor = authoring.RayleighColor,
                MieColor = authoring.MieColor,
                ScatteringColor = authoring.ScatteringColor,

                SunTextureSize = authoring.SunTextureSize,
                SunTextureIntensity = authoring.SunTextureIntensity,
                SunTextureColor = authoring.SunTextureColor,
                MoonTextureSize = authoring.MoonTextureSize,
                MoonTextureIntensity = authoring.MoonTextureIntensity,
                MoonTextureColor = authoring.MoonTextureColor,
                StarsIntensity = authoring.StarsIntensity,
                MilkyWayIntensity = authoring.MilkyWayIntensity,
                StarfieldColor = authoring.StarfieldColor,
                StarfieldRotation = authoring.StarfieldRotation,

                CloudsAltitude = authoring.CloudsAltitude,
                CloudsDirection = authoring.CloudsDirection,
                CloudsSpeed = authoring.CloudsSpeed,
                CloudsDensity = authoring.CloudsDensity,
                CloudsColor1 = authoring.CloudsColor1,
                CloudsColor2 = authoring.CloudsColor2
            });
        }
    }
}