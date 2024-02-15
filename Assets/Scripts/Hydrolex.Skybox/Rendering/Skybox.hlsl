#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define Pi 3.1415926535
#define Pi316 0.0596831
#define Pi14 0.07957747
#define MieG float3(0.4375f, 1.5625f, 1.5f)

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
uniform float   _FogScatteringScale;
uniform float  _Kr;
uniform float  _Km;
uniform float3 _Rayleigh;
uniform float3 _Mie;
uniform float  _Scattering;
uniform float  _Luminance;
uniform float  _Exposure;
uniform float4 _RayleighColor;
uniform float4 _MieColor;
uniform float4 _ScatteringColor;

// Outer Space
uniform float  _SunTextureSize;
uniform float  _SunTextureIntensity;
uniform float4 _SunTextureColor;
uniform float  _MoonTextureSize;
uniform float  _MoonTextureIntensity;
uniform float4 _MoonTextureColor;
uniform float  _StarsIntensity;
uniform float  _MilkyWayIntensity;
uniform float4 _StarFieldColor;

// Clouds
#ifdef _ENABLE_CLOUD
uniform float  _CloudAltitude;
uniform float2 _CloudDirection;
uniform float  _CloudDensity;
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
    float3 finalExtinction;
    float3 horizonExtinction;
    float3 moonExtinction;
    float3 sunExtinction;
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
    float3 cloudPos = normalize(float3(output.worldPos.x, output.worldPos.y * _CloudAltitude, output.worldPos.z));
    output.cloudUv.xy = cloudPos.xz * 0.25 - 0.005 + _CloudDirection;
    output.cloudUv.zw = cloudPos.xz * 0.35 -0.0065 + _CloudDirection;
#endif

    output.sunPos = mul((float3x3)_SunMatrix, input.positionOS.xyz) * _SunTextureSize;
    output.starPos = mul((float3x3)_StarFieldRotationMatrix, output.worldPos.xyz);
    output.starPos = mul((float3x3)_StarfieldMatrix, output.starPos);
    output.moonPos = mul((float3x3)_MoonMatrix, input.positionOS.xyz) * 0.75 * _MoonTextureSize;
    output.moonPos.x *= -1.0;

    return output;
}

///////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step1: prepare data structs for lighting calculation)
///////////////////////////////////////////////////////////////////////////////////////

SkyboxSurfaceData InitializeSurfaceData(Varyings input)
{
    SkyboxSurfaceData output = (SkyboxSurfaceData)0;

    const float r = length(float3(0.0, 50.0, 0.0));

    output.viewDirection = normalize(input.worldPos);
    output.sunCosTheta = dot(output.viewDirection, _SunDirection);
    output.moonCosTheta = dot(output.viewDirection, _MoonDirection);
    output.sunRise = saturate(dot(float3(0.0, 500.0, 0.0), _SunDirection) / r);
    output.moonRise = saturate(dot(float3(0.0, 500.0, 0.0), _MoonDirection) / r);

    const float zenith = acos(saturate(dot(float3(0.0, 1.0, 0.0), output.viewDirection)));
    const float z = cos(zenith) + 0.15 * pow(abs(93.885 - ((zenith * 180.0) / Pi)), -1.253);
    const float SR = _Kr / z;
    const float SM = _Km / z;

    const float sunset = clamp(dot(float3(0.0, 1.0, 0.0), _SunDirection), 0.0, 0.5);
    output.finalExtinction = exp(-(_Rayleigh * SR + _Mie * SM));
    output.horizonExtinction = saturate((output.viewDirection.y) * 1000.0) * output.finalExtinction.b;
    output.moonExtinction = saturate((output.viewDirection.y) * 2.5);
    output.sunExtinction = lerp(output.finalExtinction, (1.0 - output.finalExtinction), sunset);

    return output;
}

float3 GetScatteringColor(SkyboxSurfaceData data)
{
    // sun in scattering
    float rayPhase = 2.0 + 0.5 * pow(data.sunCosTheta, 2.0);                     // Rayleigh phase function based on the Nielsen's paper.
    float miePhase = MieG.x / pow(abs(MieG.y - MieG.z * data.sunCosTheta), 1.5); // The Mie phase function.
    float3 BrTheta = Pi316 * _Rayleigh * rayPhase * _RayleighColor.rgb;
    float3 BmTheta = Pi14 * _Mie * miePhase * _MieColor.rgb * data.sunRise;
    float3 BrmTheta = (BrTheta + BmTheta) / (_Rayleigh + _Mie);
    float3 sunInScatter = BrmTheta * data.sunExtinction * _Scattering * (1.0 - data.finalExtinction);
    sunInScatter *= data.sunRise;

    // moon in scattering
    rayPhase = 2.0 + 0.5 * pow(data.moonCosTheta, 2.0);
    miePhase = MieG.x / pow(abs(MieG.y - MieG.z * data.moonCosTheta), 1.5);
    BrTheta  = Pi316 * _Rayleigh * rayPhase * _RayleighColor.rgb;
    BmTheta  = Pi14  * _Mie * miePhase * _MieColor.rgb * data.moonRise;
    BrmTheta = (BrTheta + BmTheta) / (_Rayleigh + _Mie);
    float3 moonInScatter = BrmTheta * (1.0 - data.finalExtinction) * _Scattering * 0.1 * (1.0 - data.finalExtinction);
    moonInScatter *= 1.0 - data.sunRise;                                         // Diminish moon's effect when the sun is up.

    // default night sky
    BrmTheta = BrTheta / (_Rayleigh + _Mie);
    const float3 skyLuminance = BrmTheta * _ScatteringColor.rgb * _Luminance * (1.0 - data.finalExtinction);

    // combine scattering
    return sunInScatter + moonInScatter + skyLuminance;
}

float3 GetSunBaseColor(SkyboxSurfaceData data, float3 sunPos)
{
    // Sun texture
    float3 sunTexture = SAMPLE_TEXTURE2D(_SunTexture, sampler_SunTexture, sunPos.xy + 0.5).rgb * _SunTextureColor.rgb * _SunTextureIntensity;

    return pow(sunTexture, 2.0) * data.finalExtinction.b * saturate(data.sunCosTheta);
}

float3 GetMoonBaseColor(SkyboxSurfaceData data, float3 moonPos, out float moonMask)
{
    const float3 rayOrigin = float3(0.0, 0.0, 0.0);//_WorldSpaceCameraPos;
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
    moonMask = 1.0 - moonTexture.a * _MoonTextureIntensity;

    if (step(0.0, min(intersectionDistance, discriminant)) > 0.0)
    {
        const float3 normalDirection = normalize(-moonPosition + (rayOrigin + rayDirection * intersectionDistance));
        const float moonSphere = max(dot(normalDirection, _SunDirection), 0.0) * moonTexture.a * 2.0;
        moonColor = moonTexture.rgb * moonSphere * _MoonTextureColor.rgb * _MoonTextureIntensity * data.moonExtinction;
    }

    return moonColor;
}

float3 GetStarfieldColor(SkyboxSurfaceData data, float3 starPos, float moonMask)
{
    float4 starTex = SAMPLE_TEXTURECUBE(_StarfieldTexture, sampler_StarfieldTexture, starPos);
    const float3 stars = starTex.rgb * pow(starTex.a, 2.0) * _StarsIntensity;
    const float3 milkyWay = pow(abs(starTex.rgb), 1.5) * _MilkyWayIntensity;

    return  (stars + milkyWay) * _StarFieldColor.rgb * data.horizonExtinction * moonMask;
}

///////////////////////////////////////////////////////////////////////////////////////
// fragment shared functions (Step2: calculate lighting & final color)
///////////////////////////////////////////////////////////////////////////////////////

float4 frag(Varyings input) : SV_Target
{
    const SkyboxSurfaceData surfaceData = InitializeSurfaceData(input);

    // scattering color
    const float3 scattering = GetScatteringColor(surfaceData);

    // Sun texture
    float3 sunTexture = GetSunBaseColor(surfaceData, input.sunPos);

    // Moon sphere
    float moonMask = 0.0;
    float3 moonColor = GetMoonBaseColor(surfaceData, input.moonPos, moonMask);

    // Starfield
    const float3 starfield = GetStarfieldColor(surfaceData, input.starPos, moonMask);

#ifdef _ENABLE_CLOUD

    // Clouds.
    float4 tex1 = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, input.cloudUv.xy);
    float4 tex2 = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, input.cloudUv.zw);

    float3 cloud = float3(0.0, 0.0, 0.0);
    float  cloudAlpha = 1.0;
    float mixCloud = 0.0;

    if(_CloudDensity < 25)
    {
        const float noise1 = pow(abs(tex1.g + tex2.g), 0.1);
        const float noise2 = pow(abs(tex2.b * tex1.r), 0.25);

        cloudAlpha = saturate(pow(noise1 * noise2, _CloudDensity));
        float3 cloud1 = lerp(_CloudColor1.rgb, float3(0.0, 0.0, 0.0), noise1);
        float3 cloud2 = lerp(_CloudColor1.rgb, _CloudColor2.rgb, noise2) * 2.5;
        cloud = lerp(cloud1, cloud2, noise1 * noise2);

        float3 cloudLightning = lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), saturate(pow(abs(cloud), lerp(4.5, 2.25, 0.25)) * 500.0f));

        cloud += cloudLightning * _ThunderLightningEffect;
        cloudAlpha = 1.0 - cloudAlpha;
        mixCloud = saturate((surfaceData.viewDirection.y - 0.1) * pow(noise1 * noise2, _CloudDensity));
    }

    float3 output = scattering + (sunTexture + moonColor + starfield) * cloudAlpha;

    // tone mapping
    output = saturate(1.0 - exp(-_Exposure * output));

    // color correction.
    output = pow(abs(output), 2.2);

    // apply clouds
    output = lerp(output, cloud, mixCloud);

#else

    // combine color sources
    float3 output = scattering + (sunTexture + moonColor + starfield);

    // tone mapping
    output = saturate(1.0 - exp(-_Exposure * output));

    // color correction.
    output = pow(abs(output), 2.2);

#endif

    return float4(output, 1.0);
}