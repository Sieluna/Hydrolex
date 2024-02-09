using Unity.Entities;
using UnityEngine;

public class EnvironmentAuthoring : MonoBehaviour
{
    public GameObject LightTransform;
    public AnimationCurve LightIntensityCurve = EnvironmentPreset.GetDefaultLightIntensityCurve();
    public Gradient LightGradientColor = EnvironmentPreset.GetDefaultLightGradientColor();
    public AnimationCurve FlareIntensityCurve = EnvironmentPreset.GetDefaultFlareIntensityCurve();
    public AnimationCurve AmbientIntensityCurve = EnvironmentPreset.GetDefaultAmbientIntensityCurve();
    public Gradient AmbientSkyGradientColor = EnvironmentPreset.GetDefaultAmbientSkyGradientColor();
    public Gradient EquatorSkyGradientColor = EnvironmentPreset.GetDefaultEquatorSkyGradientColor();
    public Gradient GroundSkyGradientColor = EnvironmentPreset.GetDefaultGroundSkyGradientColor();

    private class EnvironmentBaker : Baker<EnvironmentAuthoring>
    {
        public override void Bake(EnvironmentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Environment
            {
                LightTransform = GetEntity(authoring.LightTransform, TransformUsageFlags.Dynamic),
                LightIntensityCurve = authoring.LightIntensityCurve.TryGetReference(this),
                LightGradientColor = authoring.LightGradientColor.TryGetReference(this),
                FlareIntensityCurve = authoring.FlareIntensityCurve.TryGetReference(this),
                AmbientIntensityCurve = authoring.AmbientIntensityCurve.TryGetReference(this),
                AmbientSkyGradientColor = authoring.AmbientSkyGradientColor.TryGetReference(this),
                EquatorSkyGradientColor = authoring.EquatorSkyGradientColor.TryGetReference(this),
                GroundSkyGradientColor = authoring.GroundSkyGradientColor.TryGetReference(this)
            });
        }
    }
}

public static class EnvironmentPreset
{
    public static AnimationCurve GetDefaultLightIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.25f),
                new(05.0f, 0.25f),
                new(06.0f, 0.05f),
                new(06.5f, 1.00f),
                new(17.0f, 1.00f),
                new(18.0f, 0.05f),
                new(19.0f, 0.25f),
                new(24.0f, 0.25f)
            }
        };
    }

    public static Gradient GetDefaultLightGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.06f, 0.25f, 0.49f), 0.22f),
                new(new Color(1.00f, 0.78f, 0.65f), 0.28f),
                new(new Color(0.89f, 0.83f, 0.75f), 0.50f),
                new(new Color(1.00f, 0.78f, 0.65f), 0.72f),
                new(new Color(0.06f, 0.25f, 0.49f), 0.78f)
            }
        };
    }

    public static AnimationCurve GetDefaultFlareIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.00f),
                new(06.5f, 0.00f),
                new(07.0f, 1.00f),
                new(17.0f, 1.00f),
                new(17.5f, 0.00f),
                new(24.0f, 0.00f)
            }
        };
    }

    public static AnimationCurve GetDefaultAmbientIntensityCurve()
    {
        return new AnimationCurve
        {
            keys = new Keyframe[]
            {
                new(00.0f, 0.30f),
                new(06.1f, 0.30f),
                new(07.6f, 0.95f),
                new(16.4f, 0.95f),
                new(17.9f, 0.30f),
                new(24.0f, 0.30f)
            }
        };
    }

    public static Gradient GetDefaultAmbientSkyGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.05f, 0.09f, 0.21f), 0.25f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.35f),
                new(new Color(1.00f, 0.75f, 0.49f), 0.50f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.65f),
                new(new Color(0.05f, 0.09f, 0.21f), 0.75f)
            }
        };
    }

    public static Gradient GetDefaultEquatorSkyGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.05f, 0.09f, 0.21f), 0.25f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.35f),
                new(new Color(1.00f, 0.75f, 0.49f), 0.50f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.65f),
                new(new Color(0.05f, 0.09f, 0.21f), 0.75f)
            }
        };
    }

    public static Gradient GetDefaultGroundSkyGradientColor()
    {
        return new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new(new Color(0.05f, 0.09f, 0.21f), 0.25f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.35f),
                new(new Color(1.00f, 0.75f, 0.49f), 0.50f),
                new(new Color(1.00f, 0.47f, 0.14f), 0.65f),
                new(new Color(0.05f, 0.09f, 0.21f), 0.75f)
            }
        };
    }
}