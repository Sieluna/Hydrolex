using Unity.Entities;
using Unity.Mathematics;

public enum CelestiumSimulation { Simple, Realistic }

public struct Celestium : IComponentData
{
    public CelestiumSimulation SimulationType;

    public Entity SunTransform;
    public Entity MoonTransform;
    public float Latitude;
    public float Longitude;
    public float Utc;

    public float3 SunLocalDirection;
    public float3 MoonLocalDirection;
}