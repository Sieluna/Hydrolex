#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 3 / (16 * pi)
#define PI316 0.0596831
// 1 / (4 * pi)
#define PI14 0.07957747

// Henyey-Greenstein phase function factor [-1, 1]
// represents the average cosine of the scattered directions
// 0 is isotropic scattering
// > 1 is forward scattering, < 1 is backwards
#define G 0.76

// Schlick phase function factor
#define K 1.55 * G - 0.55 * (G * G * G)

// note:
// subfix OS means object spaces    (e.g. positionOS = position object space)
// subfix WS means world space      (e.g. positionWS = position world space)
// subfix VS means view space       (e.g. positionVS = position view space)
// subfix CS means clip space       (e.g. positionCS = position clip space)

struct Attributes
{
    float4 positionOS     : POSITION;
};

struct Varyings
{
    float4 positionCS     : SV_POSITION;
    float3 worldPos       : TEXCOORD0;
    float3 sunPos         : TEXCOORD1;
    float3 moonPos        : TEXCOORD2;
    float3 starPos        : TEXCOORD3;
#ifdef _ENABLE_CLOUD
    float4 cloudUv        : TEXCOORD4;
#endif
};

///////////////////////////////////////////////////////////////////////////////////////
// vertex shared functions
///////////////////////////////////////////////////////////////////////////////////////

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.worldPos = normalize(mul((float3x3)GetWorldToObjectMatrix(), input.positionOS.xyz));
    output.worldPos = normalize(mul((float3x3)_UpDirectionMatrix, output.worldPos));

#ifdef _ENABLE_CLOUD
    float3 cloudPos = normalize(float3(output.worldPos.x, output.worldPos.y * _CloudData.z, output.worldPos.z));
    output.cloudUv.xy = cloudPos.xz * 0.25 - 0.005 + _CloudData.xy;
    output.cloudUv.zw = cloudPos.xz * 0.35 -0.0065 + _CloudData.xy;
#endif

    output.sunPos = mul((float3x3)_SunMatrix, input.positionOS.xyz) * _SunTextureSize;
    output.starPos = mul((float3x3)_StarFieldRotationMatrix, output.worldPos.xyz);
    output.starPos = mul((float3x3)_StarfieldMatrix, output.starPos);
    output.moonPos = mul((float3x3)_MoonMatrix, input.positionOS.xyz) * 0.75 * _MoonTextureSize;
    output.moonPos.x *= -1.0;

    return output;
}

///////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions
///////////////////////////////////////////////////////////////////////////////////////

float GetRayleighPhase(float cosTheta)
{
    return PI316 * (1.0 + pow(cosTheta, 2.0));
}

float GetHenyeyGreensteinPhase(float cosTheta, float g)
{
    return PI14 * ((1.0 - g * g) / pow(1.0 + g * g - 2.0 * g * cosTheta, 1.5));
}

float GetSchlickPhase(float cosTheta, float k)
{
    return PI14 * ((1.0 - k * k) / pow(1.0 + k * cosTheta, 2.0));
}

float3 GetScatteringColor(SkyboxSurfaceData data)
{
    // sun in scattering
    float rayPhase = GetRayleighPhase(data.sunCosTheta);
    float miePhase = GetHenyeyGreensteinPhase(data.sunCosTheta, G);
    float3 BrTheta = _Rayleigh.xyz * rayPhase * _RayleighColor.rgb;
    float3 BmTheta = _Mie.xyz * miePhase * _MieColor.rgb * data.sunRise;;
    float3 BrmTheta = (BrTheta + BmTheta) / (_Rayleigh.xyz + _Mie.xyz);
    float3 sunInScatter = BrmTheta * data.sunExtinction * _Scattering * (1.0 - data.extinction);
    sunInScatter *= data.sunRise;

    // moon in scattering
    rayPhase = GetRayleighPhase(data.moonCosTheta);
    miePhase = GetHenyeyGreensteinPhase(data.moonCosTheta, G);
    BrTheta = _Rayleigh.xyz * rayPhase * _RayleighColor.rgb;
    BmTheta = _Mie.xyz * miePhase * _MieColor.rgb * data.moonRise;
    BrmTheta = (BrTheta + BmTheta) / (_Rayleigh.xyz + _Mie.xyz);
    float3 moonInScatter = BrmTheta * (1.0 - data.extinction) * _Scattering * 0.1 * (1.0 - data.extinction);
    moonInScatter *= 1.0 - data.sunRise;                                         // Diminish moon's effect when the sun is up.

    // default night sky
    BrmTheta = BrTheta / (_Rayleigh.xyz + _Mie.xyz);
    const float3 skyLuminance = BrmTheta * _ScatteringColor.rgb * _Luminance * (1.0 - data.extinction);

    // combine scattering
    return sunInScatter + moonInScatter + skyLuminance;
}

float4 frag(Varyings input) : SV_Target
{
    const SkyboxSurfaceData surfaceData = InitializeSurfaceData(input.worldPos);

    // scattering color
    const float3 scattering = GetScatteringColor(surfaceData);

    // Sun texture
    float3 sunTexture = SampleSunBaseColor(surfaceData, input.sunPos);

    // Moon sphere
    float moonMask = 0.0;
    float3 moonColor = SampleMoonBaseColor(surfaceData, input.moonPos, moonMask);

    // Starfield
    const float3 starfield = SampleStarfieldColor(surfaceData, input.starPos, moonMask);

#ifdef _ENABLE_CLOUD

    // Clouds.
    float4 tex1 = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, input.cloudUv.xy);
    float4 tex2 = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, input.cloudUv.zw);

    float3 cloud = float3(0.0, 0.0, 0.0);
    float  cloudAlpha = 1.0;
    float mixCloud = 0.0;

    if(_CloudData.w < 25)
    {
        const float noise1 = pow(abs(tex1.g + tex2.g), 0.1);
        const float noise2 = pow(abs(tex2.b * tex1.r), 0.25);

        cloudAlpha = saturate(pow(noise1 * noise2, _CloudData.w));
        float3 cloud1 = lerp(_CloudColor1.rgb, float3(0.0, 0.0, 0.0), noise1);
        float3 cloud2 = lerp(_CloudColor1.rgb, _CloudColor2.rgb, noise2) * 2.5;
        cloud = lerp(cloud1, cloud2, noise1 * noise2);

        float3 cloudLightning = lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), saturate(pow(abs(cloud), lerp(4.5, 2.25, 0.25)) * 500.0f));

        cloud += cloudLightning * _ThunderLightningEffect;
        cloudAlpha = 1.0 - cloudAlpha;
        mixCloud = saturate((surfaceData.viewDirection.y - 0.1) * pow(noise1 * noise2, _CloudData.w));
    }

    float3 output = scattering + (sunTexture + moonColor + starfield) * cloudAlpha;

    // apply clouds
    output = lerp(output, cloud, mixCloud);

#else

    // combine color sources
    float3 output = scattering + (sunTexture + moonColor + starfield);

#endif

    return float4(output, 1.0);
}