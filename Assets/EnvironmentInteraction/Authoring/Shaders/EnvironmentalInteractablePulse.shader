Shader "Zombie Prototype/Environmental Interactable Pulse"
{
    Properties
    {
        _BaseColor("Highlight Color", Color) = (0.55, 0.82, 1.0, 1.0)
        _FlashColor("Flash Color", Color) = (1.0, 0.48, 0.08, 1.0)
        _BaseOpacity("Base Opacity", Range(0, 1)) = 0.10
        _PulseAmount("Pulse Amount", Range(0, 0.5)) = 0.08
        _PulseSpeed("Pulse Speed", Float) = 0.8
        _EmissionAmount("Emission Amount", Range(0, 2)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "EnvironmentalInteractablePulse"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FlashColor;
                half _BaseOpacity;
                half _PulseAmount;
                half _PulseSpeed;
                half _EmissionAmount;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half wave = sin(_Time.y * max(_PulseSpeed, 0.001h) * 6.2831853h);
                half pulse01 = wave * 0.5h + 0.5h;
                half opacity = saturate(_BaseOpacity + wave * _PulseAmount);
                half3 color = lerp(_BaseColor.rgb, _FlashColor.rgb, pulse01);
                color *= 1.0h + _EmissionAmount * pulse01;
                return half4(color, opacity);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
