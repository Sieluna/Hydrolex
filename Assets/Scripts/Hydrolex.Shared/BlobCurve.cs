using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

public struct KeyFrame
{
    public float Time;
    public float Value;
    public float InTangent;
    public float OutTangent;
}

public struct BlobCurve
{
    public BlobArray<KeyFrame> Keyframes;
    public BlobArray<float> Times;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(in float time)
    {
        for (var i = 0; i < Times.Length; i++)
        {
            if (time >= Times[i] && time <= Times[i + 1])
            {
                var t = math.unlerp(Keyframes[i].Time, Keyframes[i + 1].Time, time);
                var scale = Keyframes[i + 1].Time - Keyframes[i].Time;

                var hermiteBasis = new float4x4(2, -2, 1, 1, -3, 3, -2, -1, 0, 0, 1, 0, 1, 0, 0, 0);
                var parameters = new float4(t * t * t, t * t, t, 1);

                var control =
                    new float4(Keyframes[i].Value, Keyframes[i + 1].Value, Keyframes[i].OutTangent,
                        Keyframes[i + 1].InTangent) * new float4(1, 1, scale, scale);
                var basisWithParams = math.mul(parameters, hermiteBasis);
                var hermiteBlend = control * basisWithParams;

                return math.csum(hermiteBlend);
            }
        }

        return 0;
    }
}