Shader "Custom/CursedWorld" {
    Properties {
        _BlitTexture        ("Source", 2D) = "white" {}

        [Header(Desaturation)]
        _CurseAmount        ("Curse Amount (0=normal 1=full)", Range(0.0, 1.0)) = 0.0
        _DesatSpeed         ("Desaturation Speed", Range(0.0, 5.0)) = 1.0
        _ColorTint          ("Curse Color Tint", Color) = (0.15, 0.05, 0.25, 1.0)
        _TintStrength       ("Tint Strength", Range(0.0, 1.0)) = 0.6

        [Header(Chromatic Aberration)]
        _ChromaticStrength  ("Chromatic Strength", Range(0.0, 0.03)) = 0.015
        _ChromaticPulse     ("Chromatic Pulse", Range(0.0, 1.0)) = 0.3

        [Header(Vignette Creep)]
        _VignetteColor      ("Vignette Color", Color) = (0.05, 0.0, 0.1, 1.0)
        _VignetteHardness   ("Vignette Hardness", Range(0.5, 8.0)) = 3.0
        _VignetteSize       ("Vignette Size", Range(0.0, 2.0)) = 1.2

        [Header(Tendril Noise)]
        _TendrilScale       ("Tendril Scale", Range(1.0, 20.0)) = 6.0
        _TendrilSpeed       ("Tendril Speed", Range(0.0, 3.0)) = 0.8
        _TendrilStrength    ("Tendril Strength", Range(0.0, 1.0)) = 0.7
        _TendrilEdgeBias    ("Tendril Edge Bias", Range(0.5, 4.0)) = 2.0

        [Header(Pulse)]
        _PulseSpeed         ("Dark Pulse Speed", Range(0.0, 5.0)) = 1.5
        _PulseAmount        ("Dark Pulse Amount", Range(0.0, 0.3)) = 0.08

        [Header(Soul Clearing)]
        _SoulWorldPos       ("Soul World Position", Vector) = (0, 0, 0, 0)
        _ClearingRadius     ("Clearing Radius", Range(0.0, 5.0)) = 2.5
        _ClearingSharpness  ("Clearing Sharpness", Range(1.0, 20.0)) = 6.0
        _ClearingBrightness ("Clearing Brightness", Range(0.0, 1.0)) = 0.2
    }

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass {
            Name "CursedWorldPass"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half   _CurseAmount;
                half   _DesatSpeed;
                half4  _ColorTint;
                half   _TintStrength;
                half   _ChromaticStrength;
                half   _ChromaticPulse;
                half4  _VignetteColor;
                half   _VignetteHardness;
                half   _VignetteSize;
                half   _TendrilScale;
                half   _TendrilSpeed;
                half   _TendrilStrength;
                half   _TendrilEdgeBias;
                half   _PulseSpeed;
                half   _PulseAmount;
                float4 _SoulWorldPos;
                half   _ClearingRadius;
                half   _ClearingSharpness;
                half   _ClearingBrightness;
            CBUFFER_END

            half Hash(half2 p) {
                return frac(sin(dot(p, half2(127.1h, 311.7h))) * 43758.545h);
            }

            half SmoothNoise(half2 p) {
                half2 i = floor(p);
                half2 f = frac(p);
                half2 u = f * f * (3.0h - 2.0h * f);
                return lerp(
                    lerp(Hash(i),              Hash(i + half2(1,0)), u.x),
                    lerp(Hash(i + half2(0,1)), Hash(i + half2(1,1)), u.x),
                    u.y);
            }

            half FractalNoise(half2 p) {
                half val  = SmoothNoise(p)                              * 0.6h;
                     val += SmoothNoise(p * 2.1h + half2(1.7h, 9.2h))  * 0.3h;
                     val += SmoothNoise(p * 4.3h + half2(8.3h, 2.8h))  * 0.1h;
                return val;
            }

            half4 Frag(Varyings input) : SV_Target {
                half2 UV   = input.texcoord;
                half  time = (half)_Time.y;

                // early out
                if (_CurseAmount < 0.001h)
                    return half4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                 sampler_LinearClamp, UV, 0).rgb, 1.0h);

                // ----------------------------------------------------------------
                // Chromatic aberration — copied from GuinoPonzi
                // RGB channels split outward from screen center
                // scales + pulses with curse amount so it breathes with the curse
                // ----------------------------------------------------------------
                half chromaPulse = sin(time * _PulseSpeed * 1.3h) * _ChromaticPulse
                                 * 0.5h + (1.0h - _ChromaticPulse * 0.5h);
                half  chromaAmt  = _ChromaticStrength * _CurseAmount * chromaPulse;
                half2 chromaDir  = normalize(UV - 0.5h + half2(0.0001h, 0.0001h));

                half3 col;
                col.r = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV + chromaDir * chromaAmt,
                                                0).r;
                col.g = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV,
                                                0).g;
                col.b = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV - chromaDir * chromaAmt,
                                                0).b;

                // ----------------------------------------------------------------
                // Dark pulse
                // ----------------------------------------------------------------
                half pulse = sin(time * _PulseSpeed) * 0.5h + 0.5h;
                pulse     += sin(time * _PulseSpeed * 2.7h) * 0.3h;
                pulse      = pulse * _PulseAmount * _CurseAmount;

                // ----------------------------------------------------------------
                // Desaturation
                // ----------------------------------------------------------------
                half  luma   = dot(col, half3(0.299h, 0.587h, 0.114h));
                half3 desat  = half3(luma, luma, luma);
                half3 tinted = lerp(desat,
                                    desat * _ColorTint.rgb * 2.0h,
                                    _TintStrength * _CurseAmount);
                col = lerp(col, tinted, _CurseAmount);

                // ----------------------------------------------------------------
                // Vignette creep
                // ----------------------------------------------------------------
                half2 vUV     = UV - 0.5h;
                half  vDist   = length(vUV * half2(1.0h, 0.85h));
                half  vEdge   = pow(vDist * _VignetteSize, _VignetteHardness);

                half2 tUV     = UV * _TendrilScale
                              + half2(time * _TendrilSpeed * 0.3h,
                                      time * _TendrilSpeed * 0.17h);
                half  tendril = FractalNoise(tUV);

                half  edgeBias = pow(vDist * 1.5h, _TendrilEdgeBias);
                tendril       *= edgeBias * _TendrilStrength * _CurseAmount;

                half  darkMask = saturate(vEdge + tendril + pulse) * _CurseAmount;
                col            = lerp(col, _VignetteColor.rgb, darkMask);

                // ----------------------------------------------------------------
                // Soul clearing
                // ----------------------------------------------------------------
                float4 soulClip   = TransformWorldToHClip(_SoulWorldPos.xyz);
                float2 soulScreen = (soulClip.xy / soulClip.w) * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                    soulScreen.y = 1.0 - soulScreen.y;
                #endif

                float2 screenSize  = _ScreenParams.xy;
                float  aspect      = screenSize.x / screenSize.y;
                float2 soulDelta   = (float2(UV) - soulScreen)
                                   * float2(aspect, 1.0);
                float  soulDist    = length(soulDelta);

                float  clearRadius = _ClearingRadius / (screenSize.y * 0.01);
                half   clearing    = 1.0h - (half)smoothstep(0.0, clearRadius, soulDist);
                clearing           = pow(clearing, (half)_ClearingSharpness * 0.3h);
                clearing          *= _CurseAmount;

                // also reduce chroma inside the clearing — soul feels clean
                half chromaSuppress = 1.0h - clearing * 0.8h;
                col.r = lerp(col.r,
                             SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                     UV, 0).r,
                             clearing * 0.8h);
                col.b = lerp(col.b,
                             SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                     UV, 0).b,
                             clearing * 0.8h);

                half3 restored = lerp(desat, col, 1.0h);
                col             = lerp(col, restored, clearing);
                col            += clearing * _ClearingBrightness
                                * half3(0.7h, 0.8h, 1.0h);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}