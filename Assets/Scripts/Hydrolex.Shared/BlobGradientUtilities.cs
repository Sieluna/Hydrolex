using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public static class BlobGradientUtilities
{
    public static BlobAssetReference<BlobGradient> CreateBlobAssetReference(this Gradient curve, Allocator allocator = Allocator.Persistent)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref var data = ref builder.ConstructRoot<BlobGradient>();
        data.Mode = curve.mode;

        var colorKeyBuilder = builder.Allocate(ref data.ColorKeys, curve.colorKeys.Length);
        for (var i = 0; i < curve.colorKeys.Length; i++)
        {
            var item = curve.colorKeys[i];
            var j = i - 1;

            while (j >= 0 && colorKeyBuilder[j].Time > item.time)
            {
                colorKeyBuilder[j + 1] = colorKeyBuilder[j];
                j--;
            }

            colorKeyBuilder[j + 1] = new ColorKey(item);
        }

        var alphaKeyBuilder = builder.Allocate(ref data.AlphaKeys, curve.alphaKeys.Length);
        for (var i = 0; i < curve.alphaKeys.Length; i++)
        {
            var item = curve.alphaKeys[i];
            var j = i - 1;

            while (j >= 0 && alphaKeyBuilder[j].Time > item.time)
            {
                alphaKeyBuilder[j + 1] = alphaKeyBuilder[j];
                j--;
            }

            alphaKeyBuilder[j + 1] = new AlphaKey(item);
        }

        return builder.CreateBlobAssetReference<BlobGradient>(allocator);
    }

    public static BlobAssetReference<BlobGradient> TryGetReference<T>(this Gradient gradient, Baker<T> baker) where T : Component
    {
        var hash = new Hash128((uint)gradient.GetHashCode());

        if (!baker.TryGetBlobAssetReference(hash, out BlobAssetReference<BlobGradient> blobReference))
        {
            blobReference = gradient.CreateBlobAssetReference();
            baker.AddBlobAssetWithCustomHash(ref blobReference, hash);
        }

        return blobReference;
    }
}