Shader "Hydrolex/Skybox"
{
    Properties
    {
        [Toggle(_ENABLE_CLOUD)] _EnableCloud("Enable Cloud", Float) = 0.0

        _SunTexture("Sun Texture", 2D) = "white" {}
        _MoonTexture("Moon Texture", 2D) = "white" {}
        _CloudTexture("Cloud Texture", 2D) = "white" {}
        _StarfieldTexture("Starfield Texture", Cube) = "gray" {}
    }
    SubShader
    {
        Tags
        {
            // here "UniversalPipeline" tag is required, because we use urp library.
            // If Universal render pipeline is not set in the graphics settings, this SubShader will fail.

            // we can add a SubShader below or fallback to Standard built-in to make this
            // material works with both Universal Render Pipeline and Builtin-RP
            "RenderPipeline" = "UniversalPipeline"

            // explicit SubShader tag to avoid confusion
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "IgnoreProjector" = "True"
            "Queue" = "Background"
        }

        HLSLINCLUDE

        #pragma shader_feature_local _ENABLE_CLOUD

        ENDHLSL

        // [#0 Pass - Forward]
        Pass
        {
            Name "Skybox"

            // -------------------------------------
            // Render State Commands
            Fog { Mode Off }
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Includes
            #include "SkyboxInput.hlsl"
            #include "SkyboxForwardPass.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}