using Unity.Entities;
using UnityEngine;

public class SkyboxAuthoring : MonoBehaviour
{
    [Header("Scattering")]
    public Gradient RayleighGradientColor = SkyboxPreset.GetDefaultRayleighGradientColor();
    public Gradient MieGradientColor = SkyboxPreset.GetDefaultMieGradientColor();
    public AnimationCurve RayleighCurve = SkyboxPreset.GetDefaultRayleighCurve();
    public AnimationCurve MieCurve = SkyboxPreset.GetDefaultMieCurve();
    public AnimationCurve KrCurve = SkyboxPreset.GetDefaultKrCurve();
    public AnimationCurve KmCurve = SkyboxPreset.GetDefaultKmCurve();
    public AnimationCurve ScatteringCurve = SkyboxPreset.GetDefaultScatteringCurve();
    public AnimationCurve SunIntensityCurve = SkyboxPreset.GetDefaultSunIntensityCurve();
    public AnimationCurve NightIntensityCurve = SkyboxPreset.GetDefaultNightIntensityCurve();
    public AnimationCurve ExposureCurve = SkyboxPreset.GetDefaultExposureCurve();

    [Header("Night sky")]
    public Vector3 StarfieldColorBalance = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 StarfieldPosition;
    public AnimationCurve StarfieldIntensityCurve = SkyboxPreset.GetDefaultStarfieldIntensityCurve();
    public AnimationCurve MilkyWayIntensityCurve = SkyboxPreset.GetDefaultMilkyWayIntensityCurve();
    public Gradient MoonDiskGradientColor = SkyboxPreset.GetDefaultMoonDiskGradientColor();
    public Gradient MoonBrightGradientColor = SkyboxPreset.GetDefaultMoonBrightGradientColor();
    public AnimationCurve MoonBrightRangeCurve = SkyboxPreset.GetDefaultMoonBrightRangeCurve();

    [Header("Clouds")]
    public Gradient CloudGradientColor = SkyboxPreset.GetDefaultCloudGradientColor();
    public AnimationCurve CloudScatteringCurve = SkyboxPreset.GetDefaultCloudScatteringCurve();
    public AnimationCurve CloudExtinctionCurve = SkyboxPreset.GetDefaultCloudExtinctionCurve();
    public AnimationCurve CloudPowerCurve = SkyboxPreset.GetDefaultCloudPowerCurve();
    public AnimationCurve CloudIntensityCurve = SkyboxPreset.GetDefaultCloudIntensityCurve();
    [Range(-0.01f, 0.01f)] public float CloudRotationSpeed = 0.0025f;

    [Header("Celestium")]
    public float SunDiskSize = 0.5f;
    public float MoonDiskSize = 0.5f;

    private class SkyboxBaker : Baker<SkyboxAuthoring>
    {
        public override void Bake(SkyboxAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Skybox
            {
                RayleighGradientColor = authoring.RayleighGradientColor.TryGetReference(this),
                MieGradientColor = authoring.MieGradientColor.TryGetReference(this),
                RayleighCurve = authoring.RayleighCurve.TryGetReference(this),
                MieCurve = authoring.MieCurve.TryGetReference(this),
                KrCurve = authoring.KrCurve.TryGetReference(this),
                KmCurve = authoring.KmCurve.TryGetReference(this),
                ScatteringCurve = authoring.ScatteringCurve.TryGetReference(this),
                SunIntensityCurve = authoring.SunIntensityCurve.TryGetReference(this),
                NightIntensityCurve = authoring.NightIntensityCurve.TryGetReference(this),
                ExposureCurve = authoring.ExposureCurve.TryGetReference(this),

                StarfieldColorBalance = authoring.StarfieldColorBalance,
                StarfieldPosition = authoring.StarfieldPosition,
                StarfieldIntensityCurve = authoring.StarfieldIntensityCurve.TryGetReference(this),
                MilkyWayIntensityCurve = authoring.MilkyWayIntensityCurve.TryGetReference(this),
                MoonDiskGradientColor = authoring.MoonDiskGradientColor.TryGetReference(this),
                MoonBrightGradientColor = authoring.MoonBrightGradientColor.TryGetReference(this),
                MoonBrightRangeCurve = authoring.MoonBrightRangeCurve.TryGetReference(this),

                CloudGradientColor = authoring.CloudGradientColor.TryGetReference(this),
                CloudScatteringCurve = authoring.CloudScatteringCurve.TryGetReference(this),
                CloudExtinctionCurve = authoring.CloudExtinctionCurve.TryGetReference(this),
                CloudPowerCurve = authoring.CloudPowerCurve.TryGetReference(this),
                CloudIntensityCurve = authoring.CloudIntensityCurve.TryGetReference(this),
                CloudRotationSpeed = authoring.CloudRotationSpeed,
                
                SunDiskSize = authoring.SunDiskSize,
                MoonDiskSize = authoring.MoonDiskSize
            });
        }
    }
}

public static class SkyboxPreset
{
    public static Gradient GetDefaultRayleighGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.25f, 0.66f, 1.0f), 0.20f),
                new(new Color(0.61f, 0.82f, 1.0f), 0.25f),
                new(new Color(0.79f, 0.92f, 1.0f), 0.50f),
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
                new(new Color(0.99f, 0.7f, 0.52f), 0.35f),
                new(new Color(1.00f, 1.0f, 1.00f), 0.50f),
                new(new Color(0.99f, 0.7f, 0.52f), 0.65f)
            }
        };
    }

    public static AnimationCurve GetDefaultRayleighCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 2.0f),
                new(24.0f, 2.0f)
            }
        };
    }

    public static AnimationCurve GetDefaultMieCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.0f),
                new(06.0f, 0.0f),
                new(07.0f, 5.0f),
                new(08.0f, 0.5f),
                new(12.0f, 0.2f),
                new(16.0f, 0.5f),
                new(17.0f, 5.0f),
                new(18.0f, 0.0f),
                new(24.0f, 0.0f)
            }
        };
    }

    public static AnimationCurve GetDefaultKrCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 8.4f),
                new(24.0f, 8.4f)
            }
        };
    }

    public static AnimationCurve GetDefaultKmCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 1.25f),
                new(24.0f, 1.25f)
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

    public static AnimationCurve GetDefaultSunIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 3.0f),
                new(24.0f, 3.0f)
            }
        };
    }

    public static AnimationCurve GetDefaultNightIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 1.0f),
                new(05.0f, 1.0f),
                new(07.5f, 0.0f),
                new(16.5f, 0.0f),
                new(19.0f, 1.0f),
                new(24.0f, 1.0f)
            }
        };
    }

    public static AnimationCurve GetDefaultExposureCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 1.5f),
                new(24.0f, 1.5f)
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