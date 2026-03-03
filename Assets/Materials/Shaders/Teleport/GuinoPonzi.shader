Shader "Custom/DeathRespawn" {
    Properties {
        _BlitTexture            ("Source", 2D) = "white" {}

        [Header(Death)]
        _DeathProgress          ("Death Progress (0=alive 1=dead)", Range(0.0, 1.0)) = 0.0
        _DeathColor             ("Death Color", Color) = (0.05, 0.0, 0.08, 1.0)
        _DeathDesatSpeed        ("Desaturation Curve", Range(0.5, 4.0)) = 2.0
        _DeathVignetteHardness  ("Vignette Hardness", Range(1.0, 8.0)) = 3.5
        _DeathNoiseScale        ("Noise Scale", Range(1.0, 15.0)) = 6.0
        _DeathNoiseSpeed        ("Noise Speed", Range(0.0, 5.0)) = 1.5
        _ChromaticStrength      ("Chromatic Aberration", Range(0.0, 0.03)) = 0.015

        [Header(Respawn)]
        _RespawnProgress        ("Respawn Progress (0=start 1=done)", Range(0.0, 1.0)) = 0.0
        _RespawnOrigin          ("Respawn Screen Position", Vector) = (0.5, 0.5, 0, 0)
        _RespawnColor           ("Respawn Wave Color", Color) = (0.5, 0.3, 1.0, 1.0)
        _RespawnWaveWidth       ("Wave Width", Range(0.01, 0.4)) = 0.12
        _RespawnDistort         ("Wave Distortion", Range(0.0, 0.06)) = 0.03
        _RespawnGlow            ("Wave Glow", Range(0.0, 4.0)) = 2.5
        _RespawnEchoCount       ("Echo Rings", Range(0.0, 3.0)) = 2.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass {
            Name "DeathRespawnPass"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half   _DeathProgress;
                half4  _DeathColor;
                half   _DeathDesatSpeed;
                half   _DeathVignetteHardness;
                half   _DeathNoiseScale;
                half   _DeathNoiseSpeed;
                half   _ChromaticStrength;
                half   _RespawnProgress;
                float4 _RespawnOrigin;
                half4  _RespawnColor;
                half   _RespawnWaveWidth;
                half   _RespawnDistort;
                half   _RespawnGlow;
                half   _RespawnEchoCount;
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
                half v  = SmoothNoise(p)                              * 0.6h;
                     v += SmoothNoise(p * 2.1h + half2(1.7h, 9.2h))  * 0.3h;
                     v += SmoothNoise(p * 4.3h + half2(8.3h, 2.8h))  * 0.1h;
                return v;
            }

            half4 Frag(Varyings input) : SV_Target {
                half2 UV   = input.texcoord;
                half  time = (half)_Time.y;

                // early out — nothing happening
                if (_DeathProgress < 0.001h && _RespawnProgress < 0.001h)
                    return half4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                 sampler_LinearClamp, UV, 0).rgb, 1.0h);

                // ----------------------------------------------------------------
                // Chromatic aberration — RGB channels split apart
                // increases with death progress
                // ----------------------------------------------------------------
                half  chromaAmt = _ChromaticStrength * _DeathProgress;
                half2 chromaDir = normalize(UV - 0.5h + half2(0.0001h,0.0001h));

                half3 col;
                col.r = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV + chromaDir * chromaAmt * 1.0h,
                                                0).r;
                col.g = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV,
                                                0).g;
                col.b = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp,
                                                UV - chromaDir * chromaAmt * 1.0h,
                                                0).b;

                // ================================================================
                // DEATH EFFECT
                // ================================================================
                if (_DeathProgress > 0.001h) {

                    // ---- Phase 1 — 0.0..0.5 — desaturation + colour drain ----
                    half desat     = saturate(_DeathProgress * 2.0h);
                    desat          = pow(desat, _DeathDesatSpeed);
                    half  luma     = dot(col, half3(0.299h, 0.587h, 0.114h));
                    half3 desatCol = lerp(col, half3(luma,luma,luma), desat);
                    // tint toward death color
                    desatCol       = lerp(desatCol,
                                         desatCol * _DeathColor.rgb * 3.0h,
                                         desat * 0.7h);
                    col            = desatCol;

                    // ---- Phase 2 — 0.3..0.8 — vignette creeps inward ----
                    half vigT      = saturate((_DeathProgress - 0.3h) / 0.5h);
                    half2 vUV      = UV - 0.5h;
                    half  vDist    = length(vUV * half2(1.0h, 0.85h));

                    // noise warps vignette edge so it looks organic/alive
                    half2 nUV      = UV * _DeathNoiseScale
                                   + half2(time * _DeathNoiseSpeed * 0.2h,
                                           time * _DeathNoiseSpeed * 0.13h);
                    half  nVal     = FractalNoise(nUV) * vigT * 0.4h;

                    half  vigSize  = lerp(1.8h, 0.2h, vigT);
                    half  vigEdge  = pow(vDist * vigSize, _DeathVignetteHardness);
                    half  darkMask = saturate(vigEdge + nVal) * vigT;
                    col            = lerp(col, _DeathColor.rgb, darkMask);

                    // ---- Phase 3 — 0.75..1.0 — full black ----
                    half blackT    = saturate((_DeathProgress - 0.75h) / 0.25h);
                    col            = lerp(col, _DeathColor.rgb, blackT);

                    // ---- Screen shake noise (subtle UV jitter on death) ----
                    half shakeAmt  = saturate(_DeathProgress * 2.0h - 0.2h)
                                   * (1.0h - _DeathProgress) * 0.006h;
                    half2 shake    = half2(
                        Hash(half2(time * 30.h, 0.h)) - 0.5h,
                        Hash(half2(0.h, time * 30.h)) - 0.5h
                    ) * shakeAmt;
                    half3 shakeSample = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                        sampler_LinearClamp, UV + shake, 0).rgb;
                    col = lerp(col, shakeSample * lerp(half3(1,1,1),
                               half3(luma,luma,luma), desat),
                               saturate(shakeAmt * 100.h));
                }

                // ================================================================
                // RESPAWN EFFECT
                // ================================================================
                if (_RespawnProgress > 0.001h) {

                    float  aspect      = _ScreenParams.x / _ScreenParams.y;
                    float4 originClip   = TransformWorldToHClip(float3(_RespawnOrigin.xy, 0));
                    float2 originScreen = originClip.xy / originClip.w * 0.5 + 0.5;
                    #if UNITY_UV_STARTS_AT_TOP
                    originScreen.y = 1.0 - originScreen.y;
                    #endif

                    float2 delta = ((float2)UV - originScreen) * float2(aspect, 1.0);
                    half   dist        = length(delta);
                    half2  dir         = normalize(delta + half2(0.0001h,0.0001h));

                    // ---- Main ripple ring ----
                    half maxR          = 2.0h;
                    half rippleR       = _RespawnProgress * maxR;

                    half ringDist      = abs(dist - rippleR);
                    half ring          = 1.0h - smoothstep(0.0h,
                                                            _RespawnWaveWidth,
                                                            ringDist);

                    // ---- Distortion from ring ----
                    half2 distUV       = UV + dir * ring * _RespawnDistort;
                    half3 distSample   = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                         sampler_LinearClamp, distUV, 0).rgb;
                    col                = lerp(col, distSample,
                                              saturate(ring * 2.0h));

                    // ---- Color restoration behind ring ----
                    // everything the ring has already passed gets color back
                    half  passed       = step(dist, rippleR - _RespawnWaveWidth);
                    half3 restoredCol  = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                         sampler_LinearClamp, UV, 0).rgb;

                    // eases in — not instant restore
                    half  restoreAmt   = passed
                                       * smoothstep(0.0h, 0.3h, _RespawnProgress);
                    col                = lerp(col, restoredCol, restoreAmt);

                    // ---- Glow on ring ----
                    col               += _RespawnColor.rgb * ring * _RespawnGlow;

                    // ---- Echo rings trail behind main ring ----
                    for (half e = 1.0h; e <= _RespawnEchoCount; e += 1.0h) {
                        half echoOffset = e * 0.12h;
                        half echoR      = max(0.0h, rippleR - echoOffset);
                        half echoRing   = (1.0h - smoothstep(0.0h,
                                                              _RespawnWaveWidth * 1.8h,
                                                              abs(dist - echoR)))
                                        * (1.0h - e / (_RespawnEchoCount + 1.0h))
                                        * 0.4h
                                        * step(echoOffset, rippleR);
                        col            += _RespawnColor.rgb * echoRing;
                    }

                    // ---- Brightness burst at the very start ----
                    half burstT        = 1.0h - smoothstep(0.0h, 0.12h,
                                                            _RespawnProgress);
                    col               += _RespawnColor.rgb * burstT * 1.5h
                                       * (1.0h - dist * 2.0h);
                }

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}