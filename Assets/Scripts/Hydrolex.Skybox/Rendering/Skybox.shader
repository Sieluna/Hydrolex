Shader "Hydrolex/Skybox"
{
    Properties
    {
        _SunTexture("Sun Texture", 2D) = "white" {}
        _MoonTexture("Moon Texture", 2D) = "white" {}
        _CloudTexture("Cloud Texture", 2D) = "white" {}
        _StarfieldTexture("Starfield Texture", Cube) = "gray" {}
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "IgnoreProjector" = "True" }
        Cull Back
        Fog { Mode Off }
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            #pragma shader_feature_local _ENABLE_CLOUD
            #define PI 3.1415926535
            #define Pi316 0.0596831
            #define Pi14 0.07957747
            #define MieG float3(0.4375f, 1.5625f, 1.5f)

            // Textures
            TEXTURE2D(_SunTexture);             SAMPLER(sampler_SunTexture);
            TEXTURE2D(_MoonTexture);            SAMPLER(sampler_MoonTexture);
            TEXTURE2D(_CloudTexture);           SAMPLER(sampler_CloudTexture);
            TEXTURECUBE(_StarfieldTexture);     SAMPLER(sampler_StarfieldTexture);

            // Scattering
            uniform float3 _Br, _Bm;
            uniform float  _Kr, _Km;
            uniform float  _Scattering;
            uniform float  _SunIntensity;
            uniform float  _NightIntensity;
            uniform float  _Exposure;
            uniform float4 _RayleighColor;
            uniform float4 _MieColor;

            // Night sky
            uniform float4 _MoonDiskColor;
            uniform float4 _MoonBrightColor;
            uniform float  _MoonBrightRange;
            uniform float  _StarfieldIntensity;
            uniform float  _MilkyWayIntensity;
            uniform float3 _StarfieldColorBalance;

            // Clouds
            #ifdef _ENABLE_CLOUD
            uniform float4 _CloudColor;
            uniform float _CloudScattering;
            uniform float _CloudExtinction;
            uniform float _CloudPower;
            uniform float _CloudIntensity;
            uniform float _CloudRotationSpeed;
            #endif

            uniform float _SunDiskSize;
            uniform float _MoonDiskSize;

            // Directions
            uniform float3 _SunDirection;
            uniform float3 _MoonDirection;

            // Matrix
            uniform float4x4 _SkyUpDirectionMatrix;
            uniform float4x4 _SunMatrix;
            uniform float4x4 _MoonMatrix;
            uniform float4x4 _StarfieldMatrix;

            struct Attributes
            {
                float4 positionOS     : POSITION;
            };

            struct Varyings
            {
                float4 positionCS     : SV_POSITION;
                float3 localPos       : TEXCOORD0;
                float3 starfieldPos   : TEXCOORD1;
                float3 sunPos         : TEXCOORD2;
                float3 moonPos        : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.localPos = normalize(mul((float3x3)GetWorldToObjectMatrix(), input.positionOS.xyz));
                output.localPos = normalize(mul((float3x3)_SkyUpDirectionMatrix, output.localPos));

                // Matrix.
                output.sunPos = mul((float3x3)_SunMatrix, input.positionOS.xyz) * 0.75 * _SunDiskSize;
                output.starfieldPos = mul((float3x3)_SunMatrix, input.positionOS.xyz);
                output.starfieldPos = mul((float3x3)_StarfieldMatrix, output.starfieldPos);
                output.moonPos = mul((float3x3)_MoonMatrix, input.positionOS.xyz) * 0.75 * _MoonDiskSize;
                output.moonPos.x *= -1.0;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Directions.
                float r = length(float3(0.0, 50.0, 0.0));
                float3 viewDir = normalize(input.localPos);
                float sunCosTheta = dot(viewDir, _SunDirection);
                float sunRise = saturate(dot(float3(0.0, 500.0, 0.0), _SunDirection) / r);
                float moonRise = saturate(dot(float3(0.0, 500.0, 0.0), _MoonDirection) / r);

                // Optical Depth.
                float zenith = acos(saturate(dot(float3(0.0, 1.0, 0.0), viewDir)));
                float z = cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0) / PI), -1.253);
                float SR = _Kr / z;
                float SM = _Km / z;

                // Total Extinction.
                float3 fex = exp(-(_Br * SR + _Bm * SM));
                float sunset = clamp(dot(float3(0.0, 1.0, 0.0), _SunDirection), 0.0, 0.6);
                float3 extinction = lerp(fex, (1.0 - fex), sunset);

                // Sun inScattering
                float rayPhase = 2.0 + 0.5 * pow(sunCosTheta, 2.0); // Rayleigh phase function based on the Nielsen's paper.
                float miePhase = MieG.x / pow(abs(MieG.y - MieG.z * sunCosTheta), 1.5); // The Henyey-Greenstein phase function.

                float3 BrTheta = Pi316 * _Br * rayPhase * _RayleighColor.rgb * extinction;
                float3 BmTheta = Pi14 * _Bm * miePhase * _MieColor.rgb * extinction * sunRise;
                float3 BrmTheta = (BrTheta + BmTheta) / (_Br + _Bm);

                float3 inScatter = BrmTheta * _Scattering * (1.0 - fex);
                inScatter *= sunRise;

                // Night Sky.
                BrTheta = Pi316 * _Br * rayPhase * _RayleighColor.rgb;
                BrmTheta = (BrTheta) / (_Br + _Bm);
                float3 nightSky = BrmTheta * _NightIntensity * (1.0 - fex);

                float horizonExtinction = saturate((viewDir.y) * 1000.0) * fex.b;

                // Sun Disk.
                float3 sunTex = SAMPLE_TEXTURE2D(_SunTexture, sampler_SunTexture, input.sunPos.xy + 0.5).rgb *_SunIntensity;
                sunTex = pow(sunTex, 2.0);
                sunTex *= fex.b * saturate(sunCosTheta);

                // Moon Disk.
                float moonFix = saturate(dot(input.localPos, _MoonDirection)); // Delete other side moon.
                float4 moonTex = SAMPLE_TEXTURE2D(_MoonTexture, sampler_MoonTexture, input.moonPos.xy + 0.5) * moonFix;
                float moonMask = 1.0 - moonTex.a;
                float3 moonColor = (moonTex.rgb * _MoonDiskColor.rgb * moonTex.a) * horizonExtinction;

                // Moon Bright.
                float3 moonBright = 1.0 + dot(viewDir, -_MoonDirection);
                moonBright = 1.0 / (0.25 + moonBright * _MoonBrightRange) * _MoonBrightColor.rgb;
                moonBright *= moonRise;

                // Starfield.
                float4 starTex = SAMPLE_TEXTURECUBE(_StarfieldTexture, sampler_StarfieldTexture, input.starfieldPos);
                float3 stars = starTex.rgb * starTex.a;
                float3 milkyWay = pow(abs(starTex.rgb), 1.5) * _MilkyWayIntensity;
                float3 starfield = (stars + milkyWay) * _StarfieldColorBalance * moonMask * horizonExtinction *_StarfieldIntensity;

                // Clouds.
                #ifdef _ENABLE_CLOUD

                float2 cloud_uv = float2(-atan2(viewDir.z, viewDir.x), -acos(viewDir.y)) / float2(2.0 * 3.141593f, 3.141593f) + float2(-_CloudRotationSpeed, 0.0);
                float4 cloudTex = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloud_uv);
                float cloudAlpha = 1.0 - cloudTex.b;
                inScatter = inScatter + nightSky + moonBright;
                float3 cloud = lerp(inScatter * _CloudScattering, _CloudColor.rgb, cloudTex.r * pow(fex.r, _CloudExtinction)) * _CloudIntensity;
                cloud = pow(abs(cloud), _CloudPower);
                
                // Output.
                float3 output = inScatter + cloud + (sunTex + starfield + moonColor) * lerp(1.0, cloudAlpha, saturate(_CloudIntensity));

                // Tonemapping.
                output = 1.0 - exp(-_Exposure * output);
                inScatter = 1.0 - exp(-_Exposure * inScatter);

                // Calculate Cloud Extinction.
                float cloudExtinction = saturate(input.localPos.y / 0.25);
                output = lerp(output, inScatter, 1.0 - cloudExtinction);

                #else

                // Output
                float3 output = inScatter + sunTex + nightSky + starfield + moonColor + moonBright;

                // Tonemapping
                output = 1.0 - exp(-_Exposure * output);

                #endif

                // Color Correction.
                output = pow(abs(output), 2.2);

                return float4(output, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}