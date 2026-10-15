// X-ray outline of the part of the airframe removed by the cutaway section plane, so
// the aircraft's full shape stays readable while its interior is exposed.
Shader "AVIA/CutawayGhost"
{
    Properties
    {
        _GhostColor ("Ghost Color", Color) = (0.45, 0.9, 1, 0.5)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GhostColor;
            CBUFFER_END

            float4 _AviaCutPlane;
            float _AviaCutEnabled;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float side = dot(input.positionWS, _AviaCutPlane.xyz) - _AviaCutPlane.w;
                clip(_AviaCutEnabled > 0.5 ? side : -1);

                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1 - saturate(abs(dot(normalize(input.normalWS), view))), 2.5);
                return half4(_GhostColor.rgb, _GhostColor.a * (0.08 + rim));
            }
            ENDHLSL
        }
    }
}
