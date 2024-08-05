using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;

[TestFixture]
public class SkyboxSystemTests
{
    private const float k_Tolerance = 1e8f;

    public static IEnumerable RayleighTestCases
    {
        get
        {
            yield return new TestCaseData(new float3(680.0f, 550.0f, 450.0f), 2.546f)
                .SetName("WaveLength R: 680, WaveLength R: 550, WaveLength B: 450")
                .Returns((5.80e-06f, 13.56e-06f, 30.27e-06f));

            yield return new TestCaseData(new float3(650.0f, 530.0f, 470.0f), 2.546f)
                .SetName("WaveLength R: 650, WaveLength R: 530, WaveLength B: 470")
                .Returns((6.95e-06f, 15.73e-06f, 25.43e-06f));
        }
    }

    public static IEnumerable MieTestCases
    {
        get
        {
            yield return new TestCaseData(new float3(680.0f, 550.0f, 450.0f))
                .SetName("WaveLength R: 680, WaveLength R: 550, WaveLength B: 450")
                .Returns((2.09e-06f, 3.16e-06f, 4.63e-06f));

            yield return new TestCaseData(new float3(650.0f, 530.0f, 470.0f))
                .SetName("WaveLength R: 650, WaveLength R: 530, WaveLength B: 470")
                .Returns((2.29e-06f, 3.39e-06f, 4.27e-06f));
        }
    }

    public static IEnumerable CloudPositionTestCases
    {
        get
        {
            yield return new TestCaseData(0.25f, 10.0f, 0.1f, 1)
                .SetName("Initial state with 1 iterate")
                .Returns((0.00137f, 0.04998f));

            yield return new TestCaseData(0.25f, 10.0f, 0.1f, 10)
                .SetName("Initial state with 10 iterate")
                .Returns((0.01371f, 0.49981f));
        }
    }

    public static IEnumerable WorldToLocalMatrixTestCases
    {
        get
        {
            yield return new TestCaseData(LocalTransform.Identity, float4x4.identity)
                .SetName("transform identity matrix")
                .Returns(new float4x4(1f, 0f, 0f, 0f,  0f, 1f, 0f, 0f,  0f, 0f, 1f, 0f,  0f, 0f, 0f, 1f));

            yield return new TestCaseData(LocalTransform.FromRotation(quaternion.Euler(math.PI / 4.0f, 0, 0)), float4x4.identity)
                .SetName("transform rotation (45, 0, 0) matrix")
                .Returns(new float4x4(1f, 0f, 0f, 0f,  0f, 0.7f, 0.7f, 0f,  0f, -0.7f, 0.7f, 0f,  0f, 0f, 0f, 1f));

            yield return new TestCaseData(LocalTransform.FromRotation(quaternion.Euler(math.PI / 4.0f * 3.0f, 0, 0)), float4x4.identity)
                .SetName("transform rotation (135, 0, 0) matrix")
                .Returns(new float4x4(1f, 0f, 0f, 0f,  0f, -0.7f, 0.7f, 0f,  0f, -0.7f, -0.7f, 0f,  0f, 0f, 0f, 1f));
        }
    }

    [Test, TestCaseSource(nameof(RayleighTestCases))]
    public (float red, float green, float blue) ComputeRayleighCoefficientTest(float3 wavelength, float molecularDensity)
    {
        var actual = SkyboxSystem.ComputeRayleighCoefficient(wavelength, molecularDensity);

        var round = math.round(actual * k_Tolerance) / k_Tolerance;

        return (round.x, round.y, round.z);
    }

    [Test, TestCaseSource(nameof(MieTestCases))]
    public (float red, float green, float blue) ComputeMieCoefficientTest(float3 wavelength)
    {
        var actual = SkyboxSystem.ComputeMieCoefficient(wavelength);

        var round = math.round(actual * k_Tolerance) / k_Tolerance;

        return (round.x, round.y, round.z);
    }

    [Test, TestCaseSource(nameof(CloudPositionTestCases))]
    public (float x, float y) ComputeCloudPositionTest(float direction, float speed, float deltaTime, int step)
    {
        var position = float2.zero;

        for (var i = 0; i < step; i++)
            position = SkyboxSystem.ComputeCloudPosition(position, direction, speed, deltaTime);

        var round = math.round(position * 10e4f) / 10e4f;

        return (round.x, round.y);
    }

    [Test, TestCaseSource(nameof(WorldToLocalMatrixTestCases))]
    public float4x4 ComputeWorldToLocalMatrixTest(LocalTransform transform, float4x4 matrix)
    {
        var actual = SkyboxSystem.ComputeWorldToLocalMatrix(transform);

        var round = new float4x4
        {
            c0 = math.round(actual.c0 * 10.0f) / 10.0f,
            c1 = math.round(actual.c1 * 10.0f) / 10.0f,
            c2 = math.round(actual.c2 * 10.0f) / 10.0f,
            c3 = math.round(actual.c3 * 10.0f) / 10.0f
        };

        return round;
    }
}