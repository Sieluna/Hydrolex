using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Skybox : IComponentData
{
    public float MolecularDensity;
    public float3 Wavelength;
    public float Kr;
    public float Km;
    public float Rayleigh;
    public float Mie;
    public float MieDistance;
    public float Scattering;
    public float Luminance;
    public float Exposure;
    public Color RayleighColor;
    public Color MieColor;
    public Color ScatteringColor;

    public float SunTextureSize;
    public float SunTextureIntensity;
    public Color SunTextureColor;
    public float MoonTextureSize;
    public float MoonTextureIntensity;
    public Color MoonTextureColor;
    public float StarsIntensity;
    public float MilkyWayIntensity;
    public Color StarfieldColor;
    public float3 StarfieldRotation;

    public float CloudsAltitude;
    public float CloudsDirection;
    public float2 CloudsPosition;
    public float CloudsSpeed;
    public float CloudsDensity;
    public Color CloudsColor1;
    public Color CloudsColor2;
}