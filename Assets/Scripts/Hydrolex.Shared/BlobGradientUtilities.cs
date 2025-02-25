﻿using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public static class BlobGradientUtilities
{
    public static BlobAssetReference<BlobGradient> CreateBlobAssetReference(this Gradient color, Allocator allocator = Allocator.Persistent)
    {
        var builder = new BlobBuilder(Allocator.Temp);

        ref var data = ref builder.ConstructRoot<BlobGradient>();

        builder.AllocateBlobGradient(ref data, color);

        var result = builder.CreateBlobAssetReference<BlobGradient>(allocator);

        builder.Dispose();

        return result;
    }

    public static void AllocateBlobGradient(ref this BlobBuilder builder, ref BlobGradient blobGradient, Gradient color)
    {
        blobGradient.Mode = color.mode;

        var colorKeyBuilder = builder.Allocate(ref blobGradient.ColorKeys, color.colorKeys.Length);
        var alphaKeyBuilder = builder.Allocate(ref blobGradient.AlphaKeys, color.alphaKeys.Length);

        for (var i = 0; i < color.colorKeys.Length; i++)
        {
            var item = color.colorKeys[i];
            var j = i - 1;

            while (j >= 0 && colorKeyBuilder[j].Time > item.time)
            {
                colorKeyBuilder[j + 1] = colorKeyBuilder[j];
                j--;
            }

            colorKeyBuilder[j + 1] = new ColorKey(item);
        }

        for (var i = 0; i < color.alphaKeys.Length; i++)
        {
            var item = color.alphaKeys[i];
            var j = i - 1;

            while (j >= 0 && alphaKeyBuilder[j].Time > item.time)
            {
                alphaKeyBuilder[j + 1] = alphaKeyBuilder[j];
                j--;
            }

            alphaKeyBuilder[j + 1] = new AlphaKey(item);
        }
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