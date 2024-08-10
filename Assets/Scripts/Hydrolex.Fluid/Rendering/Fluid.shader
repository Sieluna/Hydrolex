Shader "Hydrolex/Fluid"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.5, 0.8, 0.95, 1.0)
        _Smoothness("Smoothness", Range(1.0, 1000.0)) = 250.0
        _RefractionStrength("Strength of the refraction", Range(0, 0.3)) = 0.01
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
            "RenderType" = "Opaque"
        }

        // [#0 Pass - Forward]
        Pass
        {
            Name "Fluid Forward"

            // -------------------------------------
            // Render State Commands
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Includes
            #include "Fluid.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}