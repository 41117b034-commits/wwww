Shader "Chapter2/Layered Forest Ground"
{
    Properties
    {
        [MainTexture] _BaseMap("Leaf litter", 2D) = "white" {}
        _LeafNormal("Leaf normal", 2D) = "bump" {}
        _LeafAO("Leaf occlusion", 2D) = "white" {}
        _SoilMap("Earth and gravel", 2D) = "white" {}
        _SoilNormal("Earth normal", 2D) = "bump" {}
        _SoilAO("Earth occlusion", 2D) = "white" {}
        _SoilRoughness("Earth roughness", 2D) = "white" {}
        _MossMap("Forest ground cover", 2D) = "white" {}
        _MossNormal("Ground cover normal", 2D) = "bump" {}
        _LeafScale("Leaf repeat metres", Float) = 2.4
        _SoilScale("Earth repeat metres", Float) = 2
        _MossScale("Ground cover repeat metres", Float) = 1.8
        _NormalStrength("Normal strength", Range(0, 2)) = .85
        _LeafTint("Leaf tint", Color) = (.78,.75,.67,1)
        _SoilTint("Earth tint", Color) = (.8,.78,.7,1)
        _MossTint("Ground cover tint", Color) = (.65,.7,.51,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _LeafTint, _SoilTint, _MossTint;
            float _LeafScale, _SoilScale, _MossScale, _NormalStrength;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half fog : TEXCOORD2;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes i)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
            o.positionCS = p.positionCS; o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(i.normalOS);
            o.fog = ComputeFogFactor(p.positionCS.z);
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForestForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_LeafNormal); SAMPLER(sampler_LeafNormal);
            TEXTURE2D(_LeafAO); SAMPLER(sampler_LeafAO);
            TEXTURE2D(_SoilMap); SAMPLER(sampler_SoilMap);
            TEXTURE2D(_SoilNormal); SAMPLER(sampler_SoilNormal);
            TEXTURE2D(_SoilAO); SAMPLER(sampler_SoilAO);
            TEXTURE2D(_SoilRoughness); SAMPLER(sampler_SoilRoughness);
            TEXTURE2D(_MossMap); SAMPLER(sampler_MossMap);
            TEXTURE2D(_MossNormal); SAMPLER(sampler_MossNormal);
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 a = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(Hash(a), Hash(a+float2(1,0)),f.x),
                            lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.positionWS.xz;
                float coarse = Noise(p*.19), fine = Noise(p*.73+8);
                // Soft irregular path, widening around the story clearing. No hard painted edge.
                float centre = -.9 + sin(p.y*.15)*.4;
                float edge = abs(p.x-centre) + (fine-.5)*1.1;
                float path = 1-smoothstep(1.05,3.45,edge);
                float clearing = 1-smoothstep(3.3,5.5,length(p-float2(0,9)));
                path = max(path,clearing*.85);
                float moss = saturate(smoothstep(.3,.73,coarse)*(1-path*.97));
                float earth = saturate(path*.66 + (1-moss)*smoothstep(.4,.7,fine)*.3);
                float leaves = max(.04,1-earth-moss);
                float3 weights = float3(leaves,earth,moss); weights /= dot(weights,1);
                float2 leafUV = p/_LeafScale;
                float2 soilUV = float2(p.x*.91-p.y*.42,p.x*.42+p.y*.91)/_SoilScale;
                float2 mossUV = p/_MossScale + float2(4.6,9.1);
                half3 leaf = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,leafUV).rgb * _LeafTint.rgb;
                half3 soil = SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,soilUV).rgb * _SoilTint.rgb;
                half3 cover = SAMPLE_TEXTURE2D(_MossMap,sampler_MossMap,mossUV).rgb * _MossTint.rgb;
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_LeafNormal,sampler_LeafNormal,leafUV));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,soilUV));
                half3 n3 = UnpackNormal(SAMPLE_TEXTURE2D(_MossNormal,sampler_MossNormal,mossUV));
                half3 n = normalize(n1*weights.x+n2*weights.y+n3*weights.z);
                half3 normal = normalize(i.normalWS + half3(n.x,0,n.y)*_NormalStrength);
                SurfaceData s = (SurfaceData)0;
                s.albedo = (leaf*weights.x+soil*weights.y+cover*weights.z)*lerp(.83,1.06,Noise(p*.08+2));
                s.alpha=1; s.metallic=0; s.normalTS=half3(0,0,1);
                s.smoothness=lerp(.08,1-SAMPLE_TEXTURE2D(_SoilRoughness,sampler_SoilRoughness,soilUV).r,weights.y)*.55;
                s.occlusion=lerp(1,SAMPLE_TEXTURE2D(_LeafAO,sampler_LeafAO,leafUV).r,weights.x*.6)
                    *lerp(1,SAMPLE_TEXTURE2D(_SoilAO,sampler_SoilAO,soilUV).r,weights.y*.5);
                InputData d=(InputData)0;
                d.positionWS=i.positionWS; d.normalWS=normal;
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI=SampleSH(normal); d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask=half4(1,1,1,1);
                d.vertexLighting=VertexLighting(i.positionWS,normal);
                half4 color=UniversalFragmentPBR(d,s);
                color.rgb=MixFog(color.rgb,i.fog);
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(Varyings i):SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            half4 NormalsFrag(Varyings i):SV_Target { return half4(normalize(i.normalWS),0); }
            ENDHLSL
        }
    }
}
