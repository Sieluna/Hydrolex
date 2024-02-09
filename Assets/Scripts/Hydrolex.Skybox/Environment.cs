using Unity.Entities;
using UnityEngine;

public struct Environment : IComponentData
{
    public Entity LightTransform;
    public float LightIntensity;
    public BlobAssetReference<BlobCurve> LightIntensityCurve;
    public Color LightColor;
    public BlobAssetReference<BlobGradient> LightGradientColor;
    public float FlareIntensity;
    public BlobAssetReference<BlobCurve> FlareIntensityCurve;
    public float AmbientIntensity;
    public BlobAssetReference<BlobCurve> AmbientIntensityCurve;
    public Color AmbientSkyColor;
    public Color EquatorSkyColor;
    public Color GroundSkyColor;
    public BlobAssetReference<BlobGradient> AmbientSkyGradientColor;
    public BlobAssetReference<BlobGradient> EquatorSkyGradientColor;
    public BlobAssetReference<BlobGradient> GroundSkyGradientColor;
}