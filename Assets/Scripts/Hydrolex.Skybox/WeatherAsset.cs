using UnityEngine;

[CreateAssetMenu(fileName = "New Weather Data", menuName = "Hydrolex/Weather", order = 0)]
public class WeatherAsset : ScriptableObject
{
    public float MolecularDensity = 0.5f;
    public AnimationCurve RayleighCurve = DefaultRayleighCurve;
    public AnimationCurve MieCurve = DefaultMieCurve;
    public Gradient RayleighGradientColor = DefaultRayleighGradientColor;
    public Gradient MieGradientColor = DefaultMieGradientColor;

    public float SunTextureIntensity = 1.0f;
    public float MoonTextureIntensity = 1.0f;
    public AnimationCurve StarsIntensityCurve = DefaultStarsIntensityCurve;
    public AnimationCurve MilkyWayIntensityCurve = DefaultMilkyWayIntensityCurve;

    public AnimationCurve LightIntensityCurve = DefaultLightIntensityCurve;
    public Gradient LightGradientColor = DefaultLightGradientColor;
    public AnimationCurve FlareIntensityCurve = DefaultFlareIntensityCurve;
    public AnimationCurve AmbientIntensityCurve = DefaultAmbientIntensityCurve;
    public Gradient AmbientSkyGradientColor = DefaultAmbientGradientColor;
    public Gradient EquatorSkyGradientColor = DefaultAmbientGradientColor;
    public Gradient GroundSkyGradientColor = DefaultAmbientGradientColor;

    public float CloudsAltitude = 0.5f;
    public float CloudsDirection = 0.0f;
    public float CloudsSpeed = 0.1f;
    public float CloudsDensity = 0.75f;
    public Gradient CloudsGradientColor1 = DefaultCloudsGradientColor1;
    public Gradient CloudsGradientColor2 = DefaultCloudsGradientColor2;

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;

            hash = hash * 31 + MolecularDensity.GetHashCode();
            hash = hash * 31 + RayleighCurve.GetHashCode();
            hash = hash * 31 + MieCurve.GetHashCode();
            hash = hash * 31 + RayleighGradientColor.GetHashCode();
            hash = hash * 31 + MieGradientColor.GetHashCode();

            hash = hash * 31 + SunTextureIntensity.GetHashCode();
            hash = hash * 31 + MoonTextureIntensity.GetHashCode();
            hash = hash * 31 + StarsIntensityCurve.GetHashCode();
            hash = hash * 31 + MilkyWayIntensityCurve.GetHashCode();

            hash = hash * 31 + LightIntensityCurve.GetHashCode();
            hash = hash * 31 + LightGradientColor.GetHashCode();
            hash = hash * 31 + FlareIntensityCurve.GetHashCode();
            hash = hash * 31 + AmbientIntensityCurve.GetHashCode();
            hash = hash * 31 + AmbientSkyGradientColor.GetHashCode();
            hash = hash * 31 + EquatorSkyGradientColor.GetHashCode();
            hash = hash * 31 + GroundSkyGradientColor.GetHashCode();

            hash = hash * 31 + CloudsAltitude.GetHashCode();
            hash = hash * 31 + CloudsDirection.GetHashCode();
            hash = hash * 31 + CloudsSpeed.GetHashCode();
            hash = hash * 31 + CloudsDensity.GetHashCode();
            hash = hash * 31 + CloudsGradientColor1.GetHashCode();
            hash = hash * 31 + CloudsGradientColor2.GetHashCode();

            return hash;
        }
    }

    public static AnimationCurve DefaultRayleighCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 0.3125f),
            new(6.0f, 0.3f),
            new(18.0f, 0.3f),
            new(24.0f, 0.3125f)
        }
    };

    public static AnimationCurve DefaultMieCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 0.500f),
            new(6.0f, 0.125f),
            new(18.0f, 0.125f),
            new(24.0f, 0.500f)
        }
    };

    public static Gradient DefaultRayleighGradientColor => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.25f, 0.66f, 1.0f), 0.2f),
            new(new Color(0.61f, 0.82f, 1.0f), 0.25f),
            new(new Color(1.0f, 1.0f, 1.0f), 0.35f),
            new(new Color(1.0f, 1.0f, 1.0f), 0.65f),
            new(new Color(0.61f, 0.82f, 1.0f), 0.75f),
            new(new Color(0.25f, 0.66f, 1.0f), 0.8f)
        }
    };

    public static Gradient DefaultMieGradientColor => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.25f, 0.659f, 1.0f), 0.225f),
            new(new Color(0.96f, 0.718f, 0.32f), 0.255f),
            new(new Color(0.96f, 0.718f, 0.32f), 0.3f),
            new(new Color(1.0f, 1.0f, 1.0f), 0.5f),
            new(new Color(0.96f, 0.718f, 0.32f), 0.7f),
            new(new Color(0.96f, 0.718f, 0.32f), 0.745f),
            new(new Color(0.25f, 0.659f, 1.0f), 0.775f)
        }
    };

    public static AnimationCurve DefaultStarsIntensityCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 1.0f),
            new(6.0f, 1.0f),
            new(6.5f, 0.0f),
            new(17.5f, 0.0f),
            new(18.0f, 1.0f),
            new(24.0f, 1.0f)
        }
    };

    public static AnimationCurve DefaultMilkyWayIntensityCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 1.0f),
            new(6.0f, 1.0f),
            new(6.5f, 0.0f),
            new(17.5f, 0.0f),
            new(18.0f, 1.0f),
            new(24.0f, 1.0f)
        }
    };

    public static AnimationCurve DefaultLightIntensityCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 0.35f),
            new(5.5f, 0.35f),
            new(6.0f, 0.01f),
            new(6.5f, 1.00f),
            new(17.5f, 1.00f),
            new(18.0f, 0.01f),
            new(18.5f, 0.35f),
            new(24.0f, 0.35f)
        }
    };

    public static Gradient DefaultLightGradientColor => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.52f, 0.68f, 1.00f), 0.225f),
            new(new Color(1.00f, 0.52f, 0.00f), 0.270f),
            new(new Color(1.00f, 1.00f, 1.00f), 0.325f),
            new(new Color(1.00f, 1.00f, 1.00f), 0.675f),
            new(new Color(1.00f, 0.52f, 0.00f), 0.730f),
            new(new Color(0.52f, 0.68f, 1.00f), 0.775f)
        }
    };

    public static AnimationCurve DefaultFlareIntensityCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 0.0f),
            new(6.8f, 0.0f),
            new(7.2f, 1.0f),
            new(16.8f, 1.0f),
            new(17.2f, 0.0f),
            new(24.0f, 0.0f)
        }
    };

    public static AnimationCurve DefaultAmbientIntensityCurve => new AnimationCurve
    {
        keys = new Keyframe[]
        {
            new(0.0f, 0.25f),
            new(5.0f, 0.25f),
            new(6.0f, 1.00f),
            new(18.0f, 1.00f),
            new(19.0f, 0.25f),
            new(24.0f, 0.25f)
        }
    };

    public static Gradient DefaultAmbientGradientColor => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.05f, 0.09f, 0.21f), 0.23f),
            new(new Color(1.00f, 0.72f, 0.54f), 0.30f),
            new(new Color(1.00f, 1.00f, 1.00f), 0.50f),
            new(new Color(1.00f, 0.72f, 0.54f), 0.70f),
            new(new Color(0.05f, 0.09f, 0.21f), 0.77f)
        }
    };

    public static Gradient DefaultCloudsGradientColor1 => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.22f, 0.44f, 0.65f), 0.23f),
            new(new Color(0.42f, 0.50f, 0.73f), 0.30f),
            new(new Color(0.42f, 0.50f, 0.73f), 0.70f),
            new(new Color(0.22f, 0.44f, 0.65f), 0.77f)
        }
    };

    public static Gradient DefaultCloudsGradientColor2 => new Gradient
    {
        colorKeys = new GradientColorKey[]
        {
            new(new Color(0.18f, 0.34f, 0.65f), 0.24f),
            new(new Color(1.00f, 0.52f, 0.00f), 0.26f),
            new(new Color(1.00f, 1.00f, 1.00f), 0.32f),
            new(new Color(1.00f, 1.00f, 1.00f), 0.68f),
            new(new Color(1.00f, 0.52f, 0.00f), 0.74f),
            new(new Color(0.18f, 0.34f, 0.65f), 0.76f)
        }
    };
}