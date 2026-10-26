Shader "MMO/Effects/Combat Particle"
{
    Properties
    {
        [MainTexture] _CombatMainTex ("Main texture", 2D) = "white" {}
        _CombatMaskTex ("Mask", 2D) = "white" {}
        _CombatSecondaryTex ("Secondary texture", 2D) = "white" {}
        _CombatNoiseTex ("UV noise", 2D) = "gray" {}
        _CombatDissolveTex ("Dissolve", 2D) = "white" {}
        [HDR] [MainColor] _CombatColor ("Particle tint", Color) = (1,1,1,1)
        _CombatMainScroll ("Main UV speed", Vector) = (0,0,0,0)
        _CombatMaskScroll ("Mask UV speed", Vector) = (0,0,0,0)
        _CombatSecondaryScroll ("Secondary UV speed", Vector) = (0,0,0,0)
        _CombatNoiseScroll ("Noise UV speed", Vector) = (0,0,0,0)
        _CombatNoiseStrength ("UV distortion", Range(0,0.2)) = 0
        _CombatOpacity ("Opacity", Range(0,1)) = 1
        _CombatEmission ("Emission", Float) = 1
        _CombatUseMask ("Use mask", Float) = 0
        _CombatUseSecondary ("Use secondary", Float) = 0
        _CombatUseDissolve ("Use dissolve", Float) = 0
        _CombatDissolve ("Dissolve threshold", Range(0,1)) = 0
        _CombatDissolveSoftness ("Dissolve edge", Range(0.001,0.25)) = 0.035
        _CombatAlphaFromLuminance ("Black background transparency", Float) = 0
        _CombatUseFresnel ("Fresnel mesh", Float) = 0
        _CombatFresnelPower ("Fresnel power", Float) = 1
        _CombatFresnelStrength ("Fresnel strength", Float) = 1
        _CombatPremultiply ("Premultiply alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _CombatSrcBlend ("Source blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _CombatDstBlend ("Destination blend", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend [_CombatSrcBlend] [_CombatDstBlend]
        Cull Off
        ZWrite Off
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _CombatMainTex, _CombatMaskTex, _CombatSecondaryTex, _CombatNoiseTex, _CombatDissolveTex;
            float4 _CombatMainTex_ST, _CombatMaskTex_ST, _CombatSecondaryTex_ST, _CombatNoiseTex_ST, _CombatDissolveTex_ST;
            float4 _CombatMainScroll, _CombatMaskScroll, _CombatSecondaryScroll, _CombatNoiseScroll;
            float4 _CombatColor;
            float _CombatNoiseStrength, _CombatOpacity, _CombatEmission, _CombatUseMask, _CombatUseSecondary;
            float _CombatUseDissolve, _CombatDissolve, _CombatDissolveSoftness, _CombatAlphaFromLuminance;
            float _CombatUseFresnel, _CombatFresnelPower, _CombatFresnelStrength, _CombatPremultiply;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 view : TEXCOORD2;
            };
            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _CombatColor;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            float2 UV(float2 uv, float4 transform, float4 scroll)
            {
                return uv * transform.xy + transform.zw + scroll.xy * _Time.y;
            }
            float4 frag(v2f i) : SV_Target
            {
                // Unsupported normal-map-only distortion materials explicitly set opacity to zero.
                clip(_CombatOpacity - 0.0001);
                float2 noiseUV = UV(i.uv, _CombatNoiseTex_ST, _CombatNoiseScroll);
                float2 warp = (tex2D(_CombatNoiseTex, noiseUV).rg * 2 - 1) * _CombatNoiseStrength;
                float4 main = tex2D(_CombatMainTex, UV(i.uv, _CombatMainTex_ST, _CombatMainScroll) + warp);
                float4 secondary = tex2D(_CombatSecondaryTex, UV(i.uv, _CombatSecondaryTex_ST, _CombatSecondaryScroll));
                main *= lerp(float4(1,1,1,1), secondary, _CombatUseSecondary);
                float alpha = main.a * lerp(1, max(main.r, max(main.g, main.b)), _CombatAlphaFromLuminance);
                float mask = tex2D(_CombatMaskTex, UV(i.uv, _CombatMaskTex_ST, _CombatMaskScroll)).r;
                alpha *= lerp(1, mask, _CombatUseMask);
                float dissolve = tex2D(_CombatDissolveTex, UV(i.uv, _CombatDissolveTex_ST, float4(0,0,0,0))).r;
                float edge = smoothstep(_CombatDissolve - _CombatDissolveSoftness, _CombatDissolve + _CombatDissolveSoftness, dissolve);
                alpha *= lerp(1, edge, _CombatUseDissolve);
                float normalView = abs(dot(i.normal, i.view)) * rsqrt(max(dot(i.normal, i.normal) * dot(i.view, i.view), 0.00000001));
                float rim = pow(1 - saturate(normalView), max(_CombatFresnelPower, 0.01));
                alpha *= lerp(1, saturate(rim * _CombatFresnelStrength), _CombatUseFresnel);
                float4 result = float4(main.rgb * i.color.rgb * _CombatEmission, saturate(alpha * i.color.a * _CombatOpacity));
                result.rgb *= lerp(1, result.a, _CombatPremultiply);
                clip(result.a - 0.0001);
                return result;
            }
            ENDCG
        }
    }
    Fallback Off
}
