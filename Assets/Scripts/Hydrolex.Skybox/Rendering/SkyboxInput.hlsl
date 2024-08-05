#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

///////////////////////////////////////////////////////////////////////////////////////
// CBUFFER and Uniforms 
// (you should put all uniforms of all passes inside this single UnityPerMaterial CBUFFER! else SRP batching is not possible!)
///////////////////////////////////////////////////////////////////////////////////////

// textures
TEXTURE2D(_SunTexture);             SAMPLER(sampler_SunTexture);
TEXTURE2D(_MoonTexture);            SAMPLER(sampler_MoonTexture);
TEXTURE2D(_CloudTexture);           SAMPLER(sampler_CloudTexture);
TEXTURECUBE(_StarfieldTexture);     SAMPLER(sampler_StarfieldTexture);

// directions
uniform float3   _SunDirection;
uniform float3   _MoonDirection;
uniform float4x4 _SunMatrix;
uniform float4x4 _MoonMatrix;
uniform float4x4 _UpDirectionMatrix;
uniform float4x4 _StarfieldMatrix;
uniform float4x4 _StarFieldRotationMatrix;

// scattering
uniform float4 _Rayleigh; // coefficient + relative optical length
uniform float4 _Mie;
uniform float  _Scattering;
uniform float  _Luminance;
uniform float  _Exposure;
uniform float4 _RayleighColor;
uniform float4 _MieColor;
uniform float4 _ScatteringColor;

// Outer Space
uniform float  _SunTextureSize;
uniform float4 _SunTextureData; // color + intensity
uniform float  _MoonTextureSize;
uniform float4 _MoonTextureData; // color + intensity
uniform float  _StarsIntensity;
uniform float  _MilkyWayIntensity;
uniform float4 _StarFieldColor;

// Clouds
#ifdef _ENABLE_CLOUD
uniform float4 _CloudData; // uv + altitude + density
uniform float4 _CloudColor1;
uniform float4 _CloudColor2;
uniform float  _ThunderLightningEffect;
#endif

struct SkyboxSurfaceData
{
    float3 viewDirection;
    float sunCosTheta;
    float moonCosTheta;
    float sunRise;
    float moonRise;
    float3 extinction;
    float3 horizonExtinction;
    float3 moonExtinction;
    float3 sunExtinction;
};

float3 SampleSunBaseColor(SkyboxSurfaceData data, float3 sunPos)
{
    // Sun texture
    const float3 sunTexture = SAMPLE_TEXTURE2D(_SunTexture, sampler_SunTexture, sunPos.xy + 0.5).rgb * _SunTextureData.rgb * _SunTextureData.a;

    return pow(sunTexture, 2.0) * data.extinction.b * saturate(data.sunCosTheta);
}

float3 SampleMoonBaseColor(SkyboxSurfaceData data, float3 moonPos, out float moonMask)
{
    const float3 rayOrigin = float3(0.0, 0.0, 0.0); // _WorldSpaceCameraPos;
    const float3 rayDirection = data.viewDirection;
    const float3 moonPosition = _MoonDirection * 38400.0 * _MoonTextureSize;
    const float moonRadius = 17370.0;

    float3 moonColor = float3(0.0, 0.0, 0.0);

    // Resolve intersection |(rayO + t * rayD) - rayP|^2 = moonR^2
    const float3 originToCenter = rayOrigin - moonPosition;
    float c = dot(originToCenter, originToCenter) - (moonRadius * moonRadius);
    float b = dot(rayDirection, originToCenter);
    const float discriminant = b * b - c;
    const float intersectionDistance = -b - sqrt(abs(discriminant));

    float4 moonTexture = saturate(SAMPLE_TEXTURE2D(_MoonTexture, sampler_MoonTexture, moonPos.xy + 0.5) * data.moonCosTheta);
    moonMask = 1.0 - moonTexture.a * _MoonTextureData.a;

    if (step(0.0, min(intersectionDistance, discriminant)) > 0.0)
    {
        const float3 normalDirection = normalize(-moonPosition + (rayOrigin + rayDirection * intersectionDistance));
        const float moonSphere = max(dot(normalDirection, _SunDirection), 0.0) * moonTexture.a * 2.0;
        moonColor = moonTexture.rgb * moonSphere * _MoonTextureData.rgb * _MoonTextureData.a * data.moonExtinction;
    }

    return moonColor;
}

float3 SampleStarfieldColor(SkyboxSurfaceData data, float3 starPos, float moonMask)
{
    float4 starTex = SAMPLE_TEXTURECUBE(_StarfieldTexture, sampler_StarfieldTexture, starPos);
    const float3 stars = starTex.rgb * pow(starTex.a, 2.0) * _StarsIntensity;
    const float3 milkyWay = pow(abs(starTex.rgb), 1.5) * _MilkyWayIntensity;

    return (stars + milkyWay) * _StarFieldColor.rgb * data.horizonExtinction * moonMask;
}

float3 CalculateFinalExtinction(float3 viewDir, float kr, float3 br, float km, float3 bm)
{
    const float zenith = acos(saturate(dot(float3(0.0, 1.0, 0.0), viewDir)));

    // The optical length s = \frac{1}{\cos(\theta_s) + 0.15(93.885 - \theta_s)^{-1.253}}
    // Iqbal, M. An Introduction to Solar Radiation. Academic Press, 1983.
    const float s = 1 / (cos(zenith) + 0.15 * pow(abs(93.885 - ((zenith * 180.0) / PI)), -1.253));
    // The optical length of the atmosphere for air molecules: 8.4 km
    const float sr = kr * s;
    // The optical length of the atmosphere for haze particle: 1.25 km
    const float sm = km * s;

    // extinction coefficient for Air: Rayleigh scattering coefficient
    // extinction coefficient for Haze: Mie scattering + Absorption coefficient
    // calculate extinction factor using Beer's Law
    return exp(-(br * sr + bm * sm));
}

SkyboxSurfaceData InitializeSurfaceData(float3 worldPos)
{
    SkyboxSurfaceData output = (SkyboxSurfaceData)0;

    const float r = length(float3(0.0, 50.0, 0.0));

    output.viewDirection = normalize(worldPos);
    output.sunCosTheta = dot(output.viewDirection, _SunDirection);
    output.moonCosTheta = dot(output.viewDirection, _MoonDirection);
    output.sunRise = saturate(dot(float3(0.0, 500.0, 0.0), _SunDirection) / r);
    output.moonRise = saturate(dot(float3(0.0, 500.0, 0.0), _MoonDirection) / r);

    output.extinction = CalculateFinalExtinction(output.viewDirection, _Rayleigh.w, _Rayleigh.xyz, _Mie.w, _Mie.xyz);

    const float sunset = clamp(dot(float3(0.0, 1.0, 0.0), _SunDirection), 0.0, 0.5);
    output.horizonExtinction = saturate(output.viewDirection.y * 1000.0) * output.extinction.b;
    output.moonExtinction = saturate(output.viewDirection.y * 2.5);
    output.sunExtinction = lerp(output.extinction, 1.0 - output.extinction, sunset);

    return output;
}