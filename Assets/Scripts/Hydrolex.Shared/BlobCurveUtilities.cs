using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public static class BlobCurveUtilities
{
    public static BlobAssetReference<BlobCurve> CreateBlobAssetReference(this AnimationCurve curve, Allocator allocator = Allocator.Persistent)
    {
        var builder = new BlobBuilder(Allocator.Temp);

        ref var data = ref builder.ConstructRoot<BlobCurve>();

        builder.AllocateBlobCurve(ref data, curve);

        var result = builder.CreateBlobAssetReference<BlobCurve>(allocator);

        builder.Dispose();

        return result;
    }

    public static void AllocateBlobCurve(ref this BlobBuilder builder, ref BlobCurve blobCurve, AnimationCurve curve)
    {
        var sortedKeyframes = new List<Keyframe>(curve.keys);
        sortedKeyframes.Sort((a, b) => a.time.CompareTo(b.time));

        var keyframeBuilder = builder.Allocate(ref blobCurve.Keyframes, sortedKeyframes.Count);
        var soaTimesBuilder = builder.Allocate(ref blobCurve.Times, sortedKeyframes.Count);

        for (var i = 0; i < sortedKeyframes.Count; i++)
        {
            keyframeBuilder[i] = sortedKeyframes[i];
            soaTimesBuilder[i] = sortedKeyframes[i].time;
        }
    }

    public static BlobAssetReference<BlobCurve> TryGetReference<T>(this AnimationCurve curve, Baker<T> baker) where T : Component
    {
        var hash = new Hash128((uint)curve.GetHashCode());

        if (!baker.TryGetBlobAssetReference(hash, out BlobAssetReference<BlobCurve> blobReference))
        {
            blobReference = curve.CreateBlobAssetReference();
            baker.AddBlobAssetWithCustomHash(ref blobReference, hash);
        }

        return blobReference;
    }
}