using Unity.Entities;
using UnityEngine;

public class CelestiumAuthoring : MonoBehaviour
{
    public GameObject SunTransform;
    public GameObject MoonTransform;
    [Range(-90f, 90f)] public float Latitude;
    [Range(-180f, 180f)] public float Longitude;
    [Range(-12f, 12f)] public float Utc;

    private class CelestiumBaker : Baker<CelestiumAuthoring>
    {
        public override void Bake(CelestiumAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Celestium
            {
                SunTransform = GetEntity(authoring.SunTransform, TransformUsageFlags.Dynamic),
                MoonTransform = GetEntity(authoring.MoonTransform, TransformUsageFlags.Dynamic),
                Latitude = authoring.Latitude,
                Longitude = authoring.Longitude,
                Utc = authoring.Utc
            });
        }
    }
}