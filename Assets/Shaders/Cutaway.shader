// Airframe shader for the aircraft viewer's cutaway mode. Everything on the positive
// side of the global section plane is discarded, and back faces seen through the cut
// are drawn in a flat section colour so the hollow skin reads as a cut edge.
Shader "AVIA/Cutaway"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _SectionColor ("Section Color", Color) = (0.20, 0.23, 0.29, 1)
        // 1 for the internal system models, which are never cut.
        _IgnoreCut ("Ignore Cut", Float) = 0
        _Emission ("Emission", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _SectionColor;
                float _IgnoreCut;
                float _Emission;
            CBUFFER_END

            // xyz: plane normal, w: plane distance. Set by AircraftViewer.
            float4 _AviaCutPlane;
            float _AviaCutEnabled;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float side = dot(input.positionWS, _AviaCutPlane.xyz) - _AviaCutPlane.w;
                if (_AviaCutEnabled > 0.5 && _IgnoreCut < 0.5)
                {
                    clip(-side);
                }

                float3 normal = normalize(input.normalWS);
                if (!isFrontFace)
                {
                    if (_AviaCutEnabled > 0.5 && _IgnoreCut < 0.5)
                    {
                        // Inside of the skin, seen through the cut: a flat section colour with
                        // light diagonal hatching so the cut face reads as a drawing convention.
                        float hatch = step(0.5, frac((input.positionWS.x + input.positionWS.y + input.positionWS.z) * 4.0));
                        return half4(_SectionColor.rgb * lerp(0.82, 1.0, hatch), 1);
                    }
                    normal = -normal;
                }

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                Light light = GetMainLight();
                half diffuse = saturate(dot(normal, light.direction)) * 0.8 + 0.2;
                half3 ambient = SampleSH(normal) * 0.6;
                half3 colour = albedo.rgb * (light.color * diffuse + ambient) + albedo.rgb * _Emission;
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
