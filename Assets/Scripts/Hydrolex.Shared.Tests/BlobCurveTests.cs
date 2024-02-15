using System.Collections;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BlobCurveTests
{
    public static IEnumerable AnimationCurveTestCases
    {
        get
        {
            yield return new TestCaseData(new Keyframe[]
                {
                    new(0, 0),
                    new(1, 1)
                })
                .SetName("Liner shape curve");

            yield return new TestCaseData(new Keyframe[]
                {
                    new(0.00f, 0.0f),
                    new(0.25f, 0.0f),
                    new(0.30f, 5.0f),
                    new(0.35f, 0.5f),
                    new(0.50f, 0.2f),
                    new(0.65f, 0.5f),
                    new(0.70f, 5.0f),
                    new(0.75f, 0.0f),
                    new(1.00f, 0.0f)
                })
                .SetName("Cat shape curve");
        }
    }

    [Test, TestCaseSource(nameof(AnimationCurveTestCases))]
    public void BlobCurveEvaluateTest(Keyframe[] keys)
    {
        var unityCurve = new AnimationCurve { keys = keys };

        using var blobCurveReference = unityCurve.CreateBlobAssetReference();

        foreach (var time in new[] { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f })
        {
            var expectedValue = unityCurve.Evaluate(time);
            var actualValue = blobCurveReference.Value.Evaluate(time);

            Assert.AreEqual(expectedValue, actualValue, 1e-6f, $"At time {time}, UnityCurve: {expectedValue}, BlobCurve: {actualValue}, Difference: {Mathf.Abs(expectedValue - actualValue)}");
        }
    }
}