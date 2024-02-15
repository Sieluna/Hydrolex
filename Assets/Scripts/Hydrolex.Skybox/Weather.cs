using Unity.Entities;

public struct BlobWeather
{
    public float MolecularDensity;
    public BlobCurve RayleighCurve;
    public BlobCurve MieCurve;
    public BlobGradient RayleighGradientColor;
    public BlobGradient MieGradientColor;

    public float SunTextureIntensity;
    public float MoonTextureIntensity;
    public BlobCurve StarsIntensityCurve;
    public BlobCurve MilkyWayIntensityCurve;

    public BlobCurve LightIntensityCurve;
    public BlobGradient LightGradientColor;
    public BlobCurve FlareIntensityCurve;
    public BlobCurve AmbientIntensityCurve;
    public BlobGradient AmbientSkyGradientColor;
    public BlobGradient EquatorSkyGradientColor;
    public BlobGradient GroundSkyGradientColor;

    public float CloudsAltitude;
    public float CloudsDirection;
    public float CloudsSpeed;
    public float CloudsDensity;
    public BlobGradient CloudsGradientColor1;
    public BlobGradient CloudsGradientColor2;
}

public struct WeatherPool
{
    public BlobArray<BlobWeather> Weathers;
    public BlobArray<BlobString> Names;
}

public struct Weather : IComponentData
{
    public BlobAssetReference<WeatherPool> WeatherPool;
    public int CurrentWeather;
}