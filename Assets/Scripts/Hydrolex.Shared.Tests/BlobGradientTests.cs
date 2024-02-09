using System.Collections;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BlobGradientTests
{
    public static IEnumerable GradientTestCases
    {
        get
        {
            yield return new TestCaseData(
                new GradientColorKey[]
                {
                    new(Color.red, 0f),
                    new(Color.blue, 1f)
                },
                new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(0f, 1f)
                }
            ).SetName("MidpointRedToBlueFadeOut");

            yield return new TestCaseData(
                new GradientColorKey[]
                {
                    new(new Color(1.0f, 1.0f, 1.0f), 0.25f),
                    new(new Color(0.0f, 0.0f, 0.0f), 0.35f),
                    new(new Color(0.0f, 0.0f, 0.0f), 0.65f),
                    new(new Color(1.0f, 1.0f, 1.0f), 0.75f),
                },
                new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(0f, 1f)
                }
            ).SetName("OneFourthWhiteBlackFadeInOut");
        }
    }

    [Test, TestCaseSource(nameof(GradientTestCases))]
    public void BlobGradientEvaluateTest(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
    {
        var unityGradient = new Gradient { colorKeys = colorKeys, alphaKeys = alphaKeys };

        using var blobGradientReference = unityGradient.CreateBlobAssetReference();

        foreach (var time in new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f })
        {
            var expectedColor = unityGradient.Evaluate(time);
            var actualColor = blobGradientReference.Value.Evaluate(time);

            AssertColorEqual(expectedColor, actualColor, 1E-6f, $"Color mismatch at time {time}");
        }
    }

    public static void AssertColorEqual(Color expected, Color actual, float delta, string message = "")
    {
        var isWithinDelta = Mathf.Abs(expected.r - actual.r) <= delta &&
                            Mathf.Abs(expected.g - actual.g) <= delta &&
                            Mathf.Abs(expected.b - actual.b) <= delta &&
                            Mathf.Abs(expected.a - actual.a) <= delta;

        Assert.IsTrue(isWithinDelta, message);
    }
}