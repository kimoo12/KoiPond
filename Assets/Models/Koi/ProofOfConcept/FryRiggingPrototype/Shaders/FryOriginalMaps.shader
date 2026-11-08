Shader "KoiPrototype/FryOriginalMaps"
{
    Properties
    {
        _BaseMap("Original Meshy Base Color", 2D) = "white" {}
        _BumpMap("Original Meshy Normal (import copy)", 2D) = "bump" {}
        _RoughnessMap("Original Meshy Roughness (linear import copy)", 2D) = "white" {}
        _BumpScale("Normal Strength", Float) = 1
        _Cull("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _BumpScale;
                float _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 tangentWS : TEXCOORD2; float2 uv : TEXCOORD3; half fog : TEXCOORD4; };
            Varyings Vert(Attributes a)
            {
                Varyings o;
                VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(a.normalOS,a.tangentOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,a.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(a.uv,_BaseMap); o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                SurfaceData s=(SurfaceData)0;
                half4 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                s.albedo=base.rgb; s.alpha=1;
                // Original metallic map was independently measured as all-zero. No borrowed/packed map.
                s.metallic=0; s.specular=half3(.04,.04,.04);
                s.smoothness=1-SAMPLE_TEXTURE2D(_RoughnessMap,sampler_RoughnessMap,i.uv).r;
                s.normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                s.occlusion=1; s.emission=0; s.clearCoatMask=0; s.clearCoatSmoothness=0;
                InputData d=(InputData)0;
                half3 tangent=normalize(i.tangentWS.xyz);
                half3 normal=normalize(i.normalWS);
                half3 bitangent=cross(normal,tangent)*i.tangentWS.w;
                d.positionWS=i.positionWS; d.normalWS=NormalizeNormalPerPixel(TransformTangentToWorld(s.normalTS,half3x3(tangent,bitangent,normal)));
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord=i.fog; d.vertexLighting=VertexLighting(i.positionWS,d.normalWS);
                d.bakedGI=SampleSH(d.normalWS); d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask=half4(1,1,1,1);
                half4 col=UniversalFragmentPBR(d,s);col.rgb=MixFog(col.rgb,i.fog); return col;
            }
            ENDHLSL
        }
    }
}
