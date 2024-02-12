using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SkyboxAuthoring : MonoBehaviour
{
    [Header("Scattering")]
    public float MolecularDensity = 2.55f;
    public float3 Wavelength = new(680, 550, 450);
    public float Kr = 8.4f;
    public float Km = 1.2f;
    public float Rayleigh = 1.5f;
    public float Mie = 1.24f;
    public float MieDistance = 0.1f;
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
    public float StarsIntensity = 0;
    public float MilkyWayIntensity = 0;
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
                MieDistance = authoring.MieDistance,
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

public static class SkyboxPreset
{
    public static AnimationCurve GetDefaultRayleighCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.25f), new(06.0f, 0.15f), new(18.0f, 0.15f), new(24.0f, 0.25f)
            }
        };
    }

    public static AnimationCurve GetDefaultMieCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.500f), new(06.0f, 0.125f), new(18.0f, 0.125f), new(24.0f, 0.500f)
            }
        };
    }
    
    public static AnimationCurve GetDefaultScatteringCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 15.0f),
                new(06.5f, 15.0f),
                new(11.0f, 24.6f),
                new(17.5f, 15.0f),
                new(24.0f, 15.0f)
            }
        };
    }

    public static Gradient GetDefaultRayleighGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.25f, 0.66f, 1.0f), 0.20f),
                new(new Color(0.61f, 0.82f, 1.0f), 0.25f),
                new(new Color(1.0f, 1.0f, 1.0f), 0.35f),
                new(new Color(1.0f, 1.0f, 1.0f), 0.65f),
                new(new Color(0.61f, 0.82f, 1.0f), 0.75f),
                new(new Color(0.25f, 0.66f, 1.0f), 0.80f)
            }
        };
    }

    public static Gradient GetDefaultMieGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.25f, 0.659f, 1.00f), 0.225f),
                new(new Color(0.96f, 0.718f, 0.32f), 0.255f),
                new(new Color(0.96f, 0.718f, 0.32f), 0.300f),
                new(new Color(1.00f, 1.000f, 1.00f), 0.500f),
                new(new Color(0.96f, 0.718f, 0.32f), 0.700f),
                new(new Color(0.96f, 0.718f, 0.32f), 0.745f),
                new(new Color(0.25f, 0.659f, 1.00f), 0.775f)
            }
        };
    }

    public static AnimationCurve GetDefaultStarfieldIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 4.0f),
                new(05.6f, 4.0f),
                new(06.6f, 0.0f),
                new(17.6f, 0.0f),
                new(18.6f, 3.9f),
                new(24.0f, 4.0f)
            }
        };
    }

    public static AnimationCurve GetDefaultMilkyWayIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.1f),
                new(24.0f, 0.1f)
            }
        };
    }

    public static Gradient GetDefaultMoonDiskGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(1.0f, 1.0f, 1.0f), 0.24f),
                new(new Color(0.0f, 0.0f, 0.0f), 0.35f),
                new(new Color(0.0f, 0.0f, 0.0f), 0.65f),
                new(new Color(1.0f, 1.0f, 1.0f), 0.76f),
            }
        };
    }

    public static Gradient GetDefaultMoonBrightGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.0f, 0.175f, 0.41f), 0.10f),
                new(new Color(0.0f, 0.0f, 0.0f), 0.24f),
                new(new Color(0.0f, 0.0f, 0.0f), 0.76f),
                new(new Color(0.0f, 0.175f, 0.41f), 0.90f),
            }
        };
    }

    public static AnimationCurve GetDefaultMoonBrightRangeCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.9f),
                new(24.0f, 0.9f)
            }
        };
    }

    public static Gradient GetDefaultCloudGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.28f, 0.35f, 0.43f), 0.245f),
                new(new Color(1.00f, 0.58f, 0.31f), 0.270f),
                new(new Color(1.00f, 0.58f, 0.31f), 0.286f),
                new(new Color(1.00f, 1.00f, 1.00f), 0.350f),
                new(new Color(1.00f, 1.00f, 1.00f), 0.650f),
                new(new Color(1.00f, 0.58f, 0.31f), 0.714f),
                new(new Color(1.00f, 0.58f, 0.31f), 0.730f),
                new(new Color(0.28f, 0.35f, 0.43f), 0.755f)
            }
        };
    }

    public static AnimationCurve GetDefaultCloudScatteringCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.00f),
                new(06.0f, 0.00f),
                new(09.0f, 1.00f),
                new(15.0f, 1.00f),
                new(18.0f, 0.00f),
                new(24.0f, 0.00f)
            }
        };
    }

    public static AnimationCurve GetDefaultCloudExtinctionCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 1.00f),
                new(06.0f, 1.00f),
                new(09.0f, 0.25f),
                new(15.0f, 0.25f),
                new(18.0f, 1.00f),
                new(24.0f, 1.00f)
            }
        };
    }

    public static AnimationCurve GetDefaultCloudPowerCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 2.20f),
                new(07.0f, 2.20f),
                new(09.0f, 4.20f),
                new(15.0f, 4.20f),
                new(17.0f, 2.20f),
                new(24.0f, 2.20f),
            }
        };
    }

    public static AnimationCurve GetDefaultCloudIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 1.50f),
                new(07.0f, 1.50f),
                new(08.0f, 1.00f),
                new(16.0f, 1.00f),
                new(17.0f, 1.50f),
                new(24.0f, 1.50f),
            }
        };
    }
}