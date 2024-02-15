using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public class WeatherAuthoring : MonoBehaviour
{
    public List<WeatherAsset> Profiles = new List<WeatherAsset>();

    private class WeatherBaker : Baker<WeatherAuthoring>
    {
        public override void Bake(WeatherAuthoring authoring)
        {
            if (authoring.Profiles.Count <= 0) return;

            foreach (var profile in authoring.Profiles) DependsOn(profile);

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var hash = (Hash128)GetProfilesHashCode(authoring.Profiles);

            if (!TryGetBlobAssetReference(hash, out BlobAssetReference<WeatherPool> poolReference))
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref var data = ref builder.ConstructRoot<WeatherPool>();
                var weathersBuilder = builder.Allocate(ref data.Weathers, authoring.Profiles.Count);
                var namesBuilder = builder.Allocate(ref data.Names, authoring.Profiles.Count);

                for (var i = 0; i < authoring.Profiles.Count; i++)
                {
                    ref var weather = ref weathersBuilder[i];
                    ref var name = ref namesBuilder[i];

                    BakeData(ref builder, ref weather, ref name, authoring.Profiles[i]);
                }

                poolReference = builder.CreateBlobAssetReference<WeatherPool>(Allocator.Persistent);

                builder.Dispose();
            }

            AddBlobAssetWithCustomHash(ref poolReference, hash);

            AddComponent(entity, new Weather
            {
                WeatherPool = poolReference,
                CurrentWeather = 0
            });
        }
    }

    public static void BakeData(ref BlobBuilder builder, ref BlobWeather weather, ref BlobString name, WeatherAsset profile)
    {
        builder.AllocateString(ref name, profile.name);

        weather.MolecularDensity = profile.MolecularDensity;
        builder.AllocateBlobCurve(ref weather.RayleighCurve, profile.RayleighCurve);
        builder.AllocateBlobCurve(ref weather.MieCurve, profile.MieCurve);
        builder.AllocateBlobGradient(ref weather.RayleighGradientColor, profile.RayleighGradientColor);
        builder.AllocateBlobGradient(ref weather.MieGradientColor, profile.MieGradientColor);

        weather.SunTextureIntensity = profile.SunTextureIntensity;
        weather.MoonTextureIntensity = profile.MoonTextureIntensity;
        builder.AllocateBlobCurve(ref weather.StarsIntensityCurve, profile.StarsIntensityCurve);
        builder.AllocateBlobCurve(ref weather.MilkyWayIntensityCurve, profile.MilkyWayIntensityCurve);

        builder.AllocateBlobCurve(ref weather.LightIntensityCurve, profile.LightIntensityCurve);
        builder.AllocateBlobGradient(ref weather.LightGradientColor, profile.LightGradientColor);
        builder.AllocateBlobCurve(ref weather.FlareIntensityCurve, profile.FlareIntensityCurve);
        builder.AllocateBlobCurve(ref weather.AmbientIntensityCurve, profile.AmbientIntensityCurve);
        builder.AllocateBlobGradient(ref weather.AmbientSkyGradientColor, profile.AmbientSkyGradientColor);
        builder.AllocateBlobGradient(ref weather.EquatorSkyGradientColor, profile.EquatorSkyGradientColor);
        builder.AllocateBlobGradient(ref weather.GroundSkyGradientColor, profile.GroundSkyGradientColor);

        weather.CloudsAltitude = profile.CloudsAltitude;
        weather.CloudsDirection = profile.CloudsDirection;
        weather.CloudsSpeed = profile.CloudsSpeed;
        weather.CloudsDensity = profile.CloudsDensity;
        builder.AllocateBlobGradient(ref weather.CloudsGradientColor1, profile.CloudsGradientColor1);
        builder.AllocateBlobGradient(ref weather.CloudsGradientColor2, profile.CloudsGradientColor2);
    }

    public static UnityEngine.Hash128 GetProfilesHashCode(List<WeatherAsset> profiles)
    {
        var hash = new UnityEngine.Hash128();

        for (var i = 0; i < profiles.Count; i++)
        {
            var profile = profiles[i];

            hash.Append(profile.GetHashCode());
        }

        return hash;
    }
}