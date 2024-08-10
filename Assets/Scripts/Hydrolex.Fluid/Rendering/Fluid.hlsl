#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

///////////////////////////////////////////////////////////////////////////////////////
// CBUFFER and Uniforms 
// (you should put all uniforms of all passes inside this single UnityPerMaterial CBUFFER! else SRP batching is not possible!)
///////////////////////////////////////////////////////////////////////////////////////

// textures
TEXTURE2D(_CameraDepthTexture);   SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_BlitTexture);          SAMPLER(sampler_BlitTexture);
TEXTURE2D(_FluidDepthTexture);    SAMPLER(sampler_FluidDepthTexture);

CBUFFER_START(UnityPerMaterial)
float4 _CameraDepthTexture_ST;
float4 _BlitTexture_ST;
float4 _FluidDepthTexture_ST;
float4 _BaseColor;
float _Smoothness;
float _RefractionStrength;
CBUFFER_END

uniform float4 _BlitScaleBias;
uniform float  _BlitMipLevel;
uniform float2 _BlitTextureSize;

// note:
// subfix OS means object spaces    (e.g. positionOS = position object space)
// subfix WS means world space      (e.g. positionWS = position world space)
// subfix VS means view space       (e.g. positionVS = position view space)
// subfix CS means clip space       (e.g. positionCS = position clip space)

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
};

Varyings vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

    output.positionCS = pos;
    output.texcoord = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

float3 ComputeViewSpacePosition(float2 positionNDC)
{
    float depth = SAMPLE_TEXTURE2D(_FluidDepthTexture, sampler_FluidDepthTexture, positionNDC).r;
    return ComputeViewSpacePosition(positionNDC, depth, UNITY_MATRIX_I_VP);
}

float4 frag(Varyings input) : SV_Target
{
    float sceneDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;

    float2 fluidDepthThickness = SAMPLE_TEXTURE2D(_FluidDepthTexture, sampler_FluidDepthTexture, input.texcoord).rg;
    float fluidDepth = fluidDepthThickness.r;
    float fluidThickness = fluidDepthThickness.g;

    if (fluidDepth > sceneDepth)
    {
        float2 texelSize = 1.0f / _ScreenSize.xy;

        float3 positionVS = ComputeViewSpacePosition(input.texcoord, fluidDepth, UNITY_MATRIX_I_VP);
        positionVS.z = -positionVS.z;

        float3 ddx = ComputeViewSpacePosition(input.texcoord + float2(texelSize.x, 0.0)) - positionVS;
        float3 ddy = ComputeViewSpacePosition(input.texcoord + float2(0.0, texelSize.y)) - positionVS;

        float3 ddx2 = positionVS - ComputeViewSpacePosition(input.texcoord + float2(-texelSize.x, 0.0));
        if (abs(ddx.z) > abs(ddx2.z)) ddx = ddx2;

        float3 ddy2 = positionVS - ComputeViewSpacePosition(input.texcoord + float2(0.0, -texelSize.y));
        if (abs(ddy.z) > abs(ddy2.z)) ddy = ddy2;

        float3 normal = normalize(cross(ddy, ddx));

        float3 viewDir = normalize(-positionVS);
        float3 lightDir = normalize(_MainLightPosition.xyz);
        float3 halfwayDir = normalize(lightDir + viewDir);

        float specular = pow(max(dot(normal, halfwayDir), 0.0f), _Smoothness);

        float3 refractionDir = refract(viewDir, normal, 1.0f / 1.333f);
        float4 transmitted = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, float2(input.texcoord + refractionDir.xy * fluidThickness * _RefractionStrength), 0.0);

        float3 transmittance = exp(-2.0f * fluidThickness * (1.0f - _BaseColor)); // Beer law
        float3 refractionColor = transmitted.rgb * transmittance;

        float3 finalColor = refractionColor + specular;

        return float4(finalColor, transmitted.a);
    }

    float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);

    return color;
}
