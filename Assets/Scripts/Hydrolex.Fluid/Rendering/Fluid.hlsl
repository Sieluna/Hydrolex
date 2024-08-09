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

float4 frag(Varyings input) : SV_Target
{
    float fluidDepth = SAMPLE_TEXTURE2D(_FluidDepthTexture, sampler_FluidDepthTexture, input.texcoord).r;
    float sceneDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;

    if (fluidDepth > sceneDepth)
    {
        return float4(1, 0, 0, 1);
    }

    float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.texcoord);

    return color;
}
