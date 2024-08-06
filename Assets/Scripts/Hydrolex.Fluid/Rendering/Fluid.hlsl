#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

///////////////////////////////////////////////////////////////////////////////////////
// CBUFFER and Uniforms 
// (you should put all uniforms of all passes inside this single UnityPerMaterial CBUFFER! else SRP batching is not possible!)
///////////////////////////////////////////////////////////////////////////////////////

// textures
TEXTURE2D(_CameraDepthTexture);   SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_BlitTexture);          SAMPLER(sampler_BlitTexture);

CBUFFER_START(UnityPerMaterial)
float4 _CameraDepthTexture_ST;
float4 _BlitTexture_ST;
CBUFFER_END

uniform float4 _BlitScaleBias;
uniform float  _BlitMipLevel;
uniform float2 _BlitTextureSize;

struct FluidData
{
    float3 position;
    float radius;
};

StructuredBuffer<FluidData> particles;

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
    UNITY_VERTEX_OUTPUT_STEREO
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
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;
    return float4(depth, depth, depth, 1);
}
