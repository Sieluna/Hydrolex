using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BlobCurve
{
    public BlobArray<Keyframe> Keyframes;
    public BlobArray<float> Times;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(in float time)
    {
        for (var i = 0; i < Times.Length; i++)
        {
            if (time >= Times[i] && time <= Times[i + 1])
            {
                var t = math.unlerp(Keyframes[i].time, Keyframes[i + 1].time, time);
                var scale = Keyframes[i + 1].time - Keyframes[i].time;

                var hermiteBasis = new float4x4(2, -2, 1, 1, -3, 3, -2, -1, 0, 0, 1, 0, 1, 0, 0, 0);
                var parameters = new float4(t * t * t, t * t, t, 1);

                var control =
                    new float4(Keyframes[i].value, Keyframes[i + 1].value, Keyframes[i].outTangent,
                        Keyframes[i + 1].inTangent) * new float4(1, 1, scale, scale);
                var basisWithParams = math.mul(parameters, hermiteBasis);
                var hermiteBlend = control * basisWithParams;

                return math.csum(hermiteBlend);
            }
        }

        return 0;
    }
}