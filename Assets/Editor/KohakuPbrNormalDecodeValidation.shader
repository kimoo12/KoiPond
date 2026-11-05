Shader "Hidden/KohakuPbrNormalDecodeValidation"
{
    Properties { _MainTex("Normal", 2D) = "bump" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(Input input)
            {
                Output output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            float4 Frag(Output input) : SV_Target
            {
                float3 n = UnpackNormalScale(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv), 1.0);
                return float4(n, 1);
            }
            ENDHLSL
        }
    }
}
