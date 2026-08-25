Shader "Custom/ToyCarColorShader"
{
    Properties
    {
        _BaseMap ("Texture Atlas", 2D) = "white" {}
        _BodyColor ("Car Body Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BodyColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // Detect Red car body pixels (R is significantly greater than G and B)
                float redMask = saturate((col.r - max(col.g, col.b)) * 2.5f);
                if (redMask > 0.01f)
                {
                    float3 recolored = _BodyColor.rgb * col.r;
                    col.rgb = lerp(col.rgb, recolored, redMask);
                }

                // Standard URP Ambient + Directional Light Shading
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                float3 ambient = SampleSH(input.normalWS);
                float3 lighting = mainLight.color * (NdotL * 0.7f) + ambient * 0.85f;

                col.rgb *= lighting;
                return col;
            }
            ENDHLSL
        }
    }
}
