using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ColorKey
{
    public Color Color;
    public float Time;

    public ColorKey(in GradientColorKey key)
    {
        Color = key.color;
        Time = key.time;
    }
}

public struct AlphaKey
{
    public float Alpha;
    public float Time;

    public AlphaKey(in GradientAlphaKey key)
    {
        Alpha = key.alpha;
        Time = key.time;
    }
}

public struct BlobGradient
{
    public BlobArray<ColorKey> ColorKeys;
    public BlobArray<AlphaKey> AlphaKeys;
    public GradientMode Mode;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Color Evaluate(in float time)
    {
        var color = Color.white;

        // Color blend
        var numColorKeys = ColorKeys.Length;
        if (numColorKeys > 1)
        {
            var timeColor = math.clamp(time, ColorKeys[0].Time, ColorKeys[numColorKeys - 1].Time);
            for (var i = 1; i < numColorKeys; ++i)
            {
                var currTime = ColorKeys[i].Time;
                if (timeColor <= currTime)
                {
                    switch (Mode)
                    {
                        case GradientMode.PerceptualBlend:
                        case GradientMode.Blend:
                            var prevTime = ColorKeys[i - 1].Time;
                            color = Color.LerpUnclamped(ColorKeys[i - 1].Color, ColorKeys[i].Color,
                                math.unlerp(prevTime, currTime, timeColor));
                            break;
                        case GradientMode.Fixed:
                            color = ColorKeys[i].Color;
                            break;
                    }

                    break;
                }
            }
        }
        else if (numColorKeys > 0)
        {
            color = ColorKeys[0].Color;
        }

        // Alpha blend
        var numAlphaKeys = AlphaKeys.Length;
        if (numAlphaKeys > 1)
        {
            var timeAlpha = math.clamp(time, AlphaKeys[0].Time, AlphaKeys[numAlphaKeys - 1].Time);
            for (var i = 1; i < numAlphaKeys; ++i)
            {
                var currTime = AlphaKeys[i].Time;
                if (timeAlpha <= currTime)
                {
                    switch (Mode)
                    {
                        case GradientMode.PerceptualBlend:
                        case GradientMode.Blend:
                        default:
                            var prevTime = AlphaKeys[i - 1].Time;
                            color.a = Mathf.Lerp(AlphaKeys[i - 1].Alpha, AlphaKeys[i].Alpha,
                                math.unlerp(prevTime, currTime, timeAlpha));
                            break;
                        case GradientMode.Fixed:
                            color.a = AlphaKeys[i].Alpha;
                            break;
                    }

                    break;
                }
            }
        }
        else if (numAlphaKeys > 0)
        {
            color.a = AlphaKeys[0].Alpha;
        }

        return color;
    }
}