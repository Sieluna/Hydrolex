using Unity.Entities;
using UnityEngine;

public struct Environment : IComponentData
{
    public Entity LightTransform;
    public float LightIntensity;
    public Color LightColor;
    public float FlareIntensity;
    public float AmbientIntensity;
    public Color AmbientSkyColor;
    public Color EquatorSkyColor;
    public Color GroundSkyColor;
}