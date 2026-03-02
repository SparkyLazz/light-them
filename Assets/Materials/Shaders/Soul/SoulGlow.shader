Shader "Custom/SoulGlow" {
    Properties {
        _MainTex            ("Sprite Texture", 2D) = "white" {}

        [Header(Core Light)]
        _CoreColor          ("Core Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _RimColor           ("Rim Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _CoreRadius         ("Core Radius", Range(0.0, 1.0)) = 0.3
        _GlowRadius         ("Glow Radius", Range(0.0, 1.0)) = 0.7
        _Brightness         ("Brightness", Range(0.0, 3.0)) = 1.5

        [Header(Pulse)]
        _PulseSpeed         ("Pulse Speed", Range(0.0, 5.0)) = 1.2
        _PulseAmount        ("Pulse Amount", Range(0.0, 1.0)) = 0.15
        _PulseFrequency     ("Pulse Frequency", Range(1.0, 8.0)) = 2.0

        [Header(Shimmer)]
        _ShimmerSpeed       ("Shimmer Speed", Range(0.0, 5.0)) = 1.5
        _ShimmerScale       ("Shimmer Scale", Range(1.0, 20.0)) = 6.0
        _ShimmerStrength    ("Shimmer Strength", Range(0.0, 1.0)) = 0.25

        [Header(Spawn Animation)]
        _SpawnTime          ("Spawn Time", Float) = 0.0
        _SpawnDuration      ("Spawn Duration", Range(0.1, 3.0)) = 1.2
        _BurstColor         ("Burst Color", Color) = (0.7, 0.9, 1.0, 1.0)
        _BurstRadius        ("Burst Radius", Range(0.5, 3.0)) = 1.8
        _BurstWidth         ("Burst Ring Width", Range(0.01, 0.5)) = 0.12

        [Header(Proximity)]
        _GoalProximity      ("Goal Proximity (0=far 1=close)", Range(0.0, 1.0)) = 0.0
        _DangerLevel        ("Danger Level (0=safe 1=danger)", Range(0.0, 1.0)) = 0.0
        _DangerColor        ("Danger Tint", Color) = (0.8, 0.3, 1.0, 1.0)
    }

    SubShader {
        Tags {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "PreviewType"     = "Plane"
        }

        Blend One One
        Cull   Off
        ZWrite Off
        ZTest  LEqual

        Pass {
            Name "SoulGlowPass"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // _ST must live OUTSIDE the CBUFFER for 2D SRP Batcher
            float4 _MainTex_ST;

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _RimColor;
                half4 _DangerColor;
                half4 _BurstColor;
                half  _CoreRadius;
                half  _GlowRadius;
                half  _Brightness;
                half  _PulseSpeed;
                half  _PulseAmount;
                half  _PulseFrequency;
                half  _ShimmerSpeed;
                half  _ShimmerScale;
                half  _ShimmerStrength;
                half  _SpawnTime;
                half  _SpawnDuration;
                half  _BurstRadius;
                half  _BurstWidth;
                half  _GoalProximity;
                half  _DangerLevel;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                half2  texcoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                half2  uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half Hash(half2 p) {
                return frac(sin(dot(p, half2(127.1h, 311.7h))) * 43758.545h);
            }

            half SmoothNoise(half2 p) {
                half2 i = floor(p);
                half2 f = frac(p);
                half2 u = f * f * (3.0h - 2.0h * f);
                half a  = Hash(i);
                half b  = Hash(i + half2(1,0));
                half c  = Hash(i + half2(0,1));
                half d  = Hash(i + half2(1,1));
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            Varyings Vert(Attributes IN) {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color      = IN.color;
                OUT.uv         = TRANSFORM_TEX(IN.texcoord, _MainTex);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target {
                half2 uv     = IN.uv;
                half2 centUV = uv - 0.5h;
                half  dist   = length(centUV);
                half  time   = (half)_Time.y;

                // ----------------------------------------------------------------
                // Spawn timeline
                // ----------------------------------------------------------------
                half elapsed   = (time - _SpawnTime) / max(_SpawnDuration, 0.001h);
                //half spawnT    = saturate(elapsed);
                //half spawnEase = 1.0h - pow(1.0h - spawnT, 3.0h);

                // ---- Phase 0 — 0.00..0.35 — coalesce from noise ----
                half coalesceT    = saturate(elapsed / 0.35h);
                half coalesceEase = coalesceT * coalesceT;

                half2 noiseUV = centUV * 8.h
                              + half2(time * 0.4h, time * 0.3h);
                half noiseVal = SmoothNoise(noiseUV);
                half dissolve = step(noiseVal, coalesceEase * 1.2h);

                // ---- Phase 1 — 0.20..0.70 — scale up ----
                half scaleT    = saturate((elapsed - 0.2h) / 0.5h);
                half scaleEase = 1.0h - pow(1.0h - scaleT, 2.5h);
                half scaledDist = dist / max(scaleEase, 0.001h);
                half useDist    = lerp(scaledDist, dist, scaleEase);

                // ---- Phase 2 — 0.45..1.00 — burst ring ----
                half burstT    = saturate((elapsed - 0.45h) / 0.55h);
                half burstEase = burstT * (2.0h - burstT);
                half burstR    = burstEase * _BurstRadius;
                half burstRing = 1.0h - smoothstep(0.0h, _BurstWidth,
                                                    abs(dist - burstR));
                burstRing     *= (1.0h - burstEase);
                burstRing     *= saturate(elapsed - 0.45h) * step(elapsed, 1.8h);

                // ---- Phase 3 — 0.70..1.20 — settle shimmer ----
                half settleT   = saturate((elapsed - 0.7h) / 0.5h);
                half settleShim = (1.0h - settleT) * 0.4h;

                // ----------------------------------------------------------------
                // Idle glow
                // ----------------------------------------------------------------

                // ---- Pulse ----
                half pulseSpeed = 1.0h + _GoalProximity * 2.5h;
                half pulse = sin(time * _PulseSpeed * _PulseFrequency) * 0.6h
                           + sin(time * _PulseSpeed * _PulseFrequency * 2.3h) * 0.4h;
                pulse  = pulse * _PulseAmount * 0.5h;
                pulse += sin(time * _PulseSpeed * pulseSpeed)
                       * _GoalProximity * 0.08h;

                // ---- Shimmer ----
                half2 shimUV = centUV * _ShimmerScale
                             + half2(time * _ShimmerSpeed * 0.3h,
                                     time * _ShimmerSpeed * 0.17h);
                half shimmer  = SmoothNoise(shimUV) * _ShimmerStrength;
                shimmer      += settleShim;

                half distWarped = useDist - shimmer * 0.08h - pulse * useDist;

                // ---- Radial layers ----
                half core = 1.0h - smoothstep(_CoreRadius * 0.3h,
                                               _CoreRadius, distWarped);

                half midGlow = pow(max(0.h, 1.0h - smoothstep(_CoreRadius,
                                                                _GlowRadius,
                                                                distWarped)), 2.0h);

                half rim = pow(max(0.h, 1.0h - smoothstep(_GlowRadius * 0.7h,
                                                            _GlowRadius * 1.1h,
                                                            distWarped)), 3.5h);

                // ---- Colour ----
                half3 col  = _RimColor.rgb  * rim;
                col       += _CoreColor.rgb * midGlow * 0.8h;
                col       += half3(1,1,1)   * core;

                // goal proximity
                half goalGlow = _GoalProximity * 0.5h
                              * (sin(time * 6.h * pulseSpeed) * 0.5h + 0.5h);
                col += half3(0.6h, 0.8h, 1.0h) * goalGlow;

                // danger rim
                half dangerRim = _DangerLevel
                               * (1.0h - smoothstep(0.2h, _GlowRadius, distWarped))
                               * (sin(time * 8.h) * 0.5h + 0.5h);
                col = lerp(col, _DangerColor.rgb * col, dangerRim * 0.6h);

                // ----------------------------------------------------------------
                // Spawn composite
                // ----------------------------------------------------------------
                half spawnMask = lerp(dissolve, 1.0h, coalesceEase);
                col           *= spawnMask;
                col           += _BurstColor.rgb * burstRing * 2.5h;

                // ---- Sprite mask ----
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half  mask   = sprite.a * (1.0h - smoothstep(0.45h, 0.5h, dist));

                col *= _Brightness * (half3)IN.color.rgb;

                return half4(col, saturate(mask * (rim + midGlow) * spawnMask));
            }
            ENDHLSL
        }
    }
}