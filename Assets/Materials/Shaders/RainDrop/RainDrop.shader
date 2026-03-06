Shader "Custom/Raindrop"
{
    Properties
    {
        [HideInInspector] _BlitTexture("Source (auto-bound)", 2D) = "white" {}

        [Header(Quality)]
        [KeywordEnum(Extreme, Ultra, High, Medium, Low)]
        _Quality("Quality Tier", Float) = 2

        [Header(Rain Layers)]
        _RainAmount         ("Rain Amount",              Range(0.0, 1.0))  = 0.7
        _TimeScale          ("Time Scale",               Range(0.0, 2.0))  = 0.2
        _DropSpeed          ("Drop Fall Speed",          Range(0.0, 3.0))  = 0.75
        _GridColumns        ("Grid Columns",             Range(1.0, 16.0)) = 6.0
        _WiggleStrength     ("Wiggle Strength",          Range(0.0, 1.0))  = 0.7
        _DropSize           ("Drop Size",                Range(0.1, 1.0))  = 0.4
        _TrailWidth         ("Trail Width",              Range(0.0, 1.0))  = 0.23
        _StaticGridScale    ("Static Drop Grid Scale",   Range(10.0, 80.0))= 40.0
        _Layer2Scale        ("Layer 2 UV Scale",         Range(1.0, 4.0))  = 1.85
        _Layer3Scale        ("Layer 3 UV Scale (Extreme)",Range(1.5, 5.0)) = 2.5

        [Header(Blur)]
        _MaxBlur            ("Max Blur (dry glass)",     Range(0.0, 12.0)) = 5.0
        _MinBlur            ("Min Blur (through drop)",  Range(0.0, 4.0))  = 2.0

        [Header(Chromatic Aberration)]
        _ChromaStrength     ("Chroma Strength",          Range(0.0, 0.02)) = 0.005
        _ChromaSpread       ("Chroma Spread",            Range(0.0, 4.0))  = 1.8

        [Header(Color Grade)]
        _ColorGradeTint     ("Grade Tint",               Color)            = (0.80, 0.91, 1.28, 1.0)
        _ColorGradeStrength ("Grade Strength",           Range(0.0, 1.0))  = 0.55
        _ColorGradeDrift    ("Grade Drift Speed",        Range(0.0, 1.0))  = 0.07

        [Header(Vignette)]
        _VignetteIntensity  ("Vignette Intensity",       Range(0.0, 4.0))  = 1.8

        [Header(Fade In)]
        _FadeInDuration     ("Fade-in Duration (sec)",   Range(0.0, 10.0)) = 3.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            Name "Raindrop"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile_local _QUALITY_EXTREME _QUALITY_ULTRA _QUALITY_HIGH _QUALITY_MEDIUM _QUALITY_LOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                // Rain
                float  _RainAmount;
                float  _TimeScale;
                float  _DropSpeed;
                float  _GridColumns;
                float  _WiggleStrength;
                float  _DropSize;
                float  _TrailWidth;
                float  _StaticGridScale;
                float  _Layer2Scale;
                float  _Layer3Scale;
                // Blur
                float  _MaxBlur;
                float  _MinBlur;
                // Chromatic aberration
                float  _ChromaStrength;
                float  _ChromaSpread;
                // Color grade
                float4 _ColorGradeTint;
                float  _ColorGradeStrength;
                float  _ColorGradeDrift;
                // Vignette
                float  _VignetteIntensity;
                // Fade
                float  _FadeInDuration;
            CBUFFER_END

            // ── Quality feature flags ─────────────────────────────────────
            // Numeric values now come from properties above.
            // Flags only toggle which code blocks compile in.
            #if defined(_QUALITY_EXTREME)
                #define EXTRA_LAYER
                #define COLOR_GRADE
                #define CHROMA_ABERR
                #define VIGNETTE

            #elif defined(_QUALITY_ULTRA)
                #define COLOR_GRADE
                #define VIGNETTE

            #elif defined(_QUALITY_HIGH)
                #define VIGNETTE

            #elif defined(_QUALITY_MEDIUM)
                #define CHEAP_NORMALS
                #define VIGNETTE

            #else // LOW
                #define CHEAP_NORMALS
                #define SKIP_POST_FX
            #endif

            // ── Noise ─────────────────────────────────────────────────────
            float3 N13(float p)
            {
                float3 p3 = frac(float3(p, p, p) * float3(0.1031, 0.11369, 0.13787));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac(float3(
                    (p3.x + p3.y) * p3.z,
                    (p3.x + p3.z) * p3.y,
                    (p3.y + p3.z) * p3.x));
            }

            float N(float t)
            {
                return frac(sin(t * 12345.564) * 7658.76);
            }

            float Saw(float b, float t)
            {
                return smoothstep(0.0, b, t) * smoothstep(1.0, b, t);
            }

            // ── Drop simulation ───────────────────────────────────────────
            float2 DropLayer2(float2 uv, float t)
            {
                float2 UV   = uv;
                uv.y       += t * _DropSpeed;

                float2 a    = float2(_GridColumns, 1.0);
                float2 grid = a * 2.0;
                float2 id   = floor(uv * grid);

                uv.y += N(id.x);
                id    = floor(uv * grid);

                float3 n  = N13(id.x * 35.2 + id.y * 2376.1);
                float2 st = frac(uv * grid) - float2(0.5, 0.0);

                float x      = n.x - 0.5;
                float y      = UV.y * 20.0;
                float wiggle = sin(y + sin(y));
                x += wiggle * (0.5 - abs(x)) * (n.z - 0.5);
                x *= _WiggleStrength;

                float ti = frac(t + n.z);
                y = (Saw(0.85, ti) - 0.5) * 0.9 + 0.5;

                float2 p = float2(x, y);

                float d        = length((st - p) * a.yx);
                float mainDrop = smoothstep(_DropSize, 0.0, d);

                float r          = sqrt(smoothstep(1.0, y, st.y));
                float cd         = abs(st.x - x);
                float trail      = smoothstep(_TrailWidth * r, 0.15 * r * r, cd);
                float trailFront = smoothstep(-0.02, 0.02, st.y - y);
                trail *= trailFront * r * r;

                float2 beadUV  = float2(st.x, frac(UV.y * 10.0) + (st.y - 0.5));
                float  droplets = smoothstep(0.3, 0.0, length(beadUV - float2(x, y)));

                float m = mainDrop + droplets * r * trailFront;
                return float2(m, trail);
            }

            float StaticDrops(float2 uv, float t)
            {
                uv *= _StaticGridScale;
                float2 id  = floor(uv);
                uv         = frac(uv) - 0.5;
                float3 n   = N13(id.x * 107.45 + id.y * 3543.654);
                float2 p   = (n.xy - 0.5) * 0.7;
                float  d   = length(uv - p);
                float fade = Saw(0.025, frac(t + n.z));
                return smoothstep(0.3, 0.0, d) * frac(n.z * 10.0) * fade;
            }

            float2 Drops(float2 uv, float t, float l0, float l1, float l2)
            {
                float  s  = StaticDrops(uv, t) * l0;
                float2 m1 = DropLayer2(uv,             t) * l1;
                float2 m2 = DropLayer2(uv * _Layer2Scale, t) * l2;

                #if defined(EXTRA_LAYER)
                    float  ew    = smoothstep(0.1, 0.6, l2);
                    float2 m3    = DropLayer2(uv * _Layer3Scale, t) * ew;
                    float  c     = s + m1.x + m2.x + m3.x;
                    float  trail = max(max(m1.y * l0, m2.y * l1), m3.y * ew);
                #else
                    float  c     = s + m1.x + m2.x;
                    float  trail = max(m1.y * l0, m2.y * l1);
                #endif

                c = smoothstep(0.3, 1.0, c);
                return float2(c, trail);
            }

            float3 SampleScene(float2 uv, float lod)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, lod).rgb;
            }

            // ── Fragment ──────────────────────────────────────────────────
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 UV     = input.texcoord;
                float  aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv     = (UV - 0.5) * float2(aspect, 1.0);

                float T          = _Time.y;
                float t          = T * _TimeScale;
                float rainAmount = _RainAmount;

                float staticDrops = smoothstep(-0.5, 1.0, rainAmount) * 2.0;
                float layer1      = smoothstep(0.25, 0.75, rainAmount);
                float layer2      = smoothstep(0.0,  0.5,  rainAmount);

                float2 c = Drops(uv, t, staticDrops, layer1, layer2);

                float2 n;
                #if defined(CHEAP_NORMALS)
                    n = float2(ddx(c.x), ddy(c.x));
                #else
                    float2 e  = float2(0.001, 0.0);
                    float  cx = Drops(uv + e,    t, staticDrops, layer1, layer2).x;
                    float  cy = Drops(uv + e.yx, t, staticDrops, layer1, layer2).x;
                    n = float2(cx - c.x, cy - c.x);
                #endif

                float  focus  = lerp(_MaxBlur - c.y, _MinBlur, smoothstep(0.1, 0.2, c.x));
                float2 uvWarp = UV + n;

                float3 col;
                #if defined(CHROMA_ABERR)
                    float  ca   = saturate(length(n) * _ChromaSpread);
                    float2 dir  = normalize(n + 1e-5);
                    float3 colR = SampleScene(uvWarp + dir * ca * _ChromaStrength, focus);
                    float3 colG = SampleScene(uvWarp,                              focus);
                    float3 colB = SampleScene(uvWarp - dir * ca * _ChromaStrength, focus);
                    col = float3(colR.r, colG.g, colB.b);
                #else
                    col = SampleScene(uvWarp, focus);
                #endif

                #if !defined(SKIP_POST_FX)

                    #if defined(COLOR_GRADE)
                        float drift = sin(T * _ColorGradeDrift) * 0.5 + 0.5;
                        col = lerp(col,
                                   col * _ColorGradeTint.rgb,
                                   drift * rainAmount * _ColorGradeStrength);
                    #endif

                    #if defined(VIGNETTE)
                        float2 vigUV = UV - 0.5;
                        col *= saturate(1.0 - dot(vigUV, vigUV) * _VignetteIntensity);
                    #endif

                    col *= smoothstep(0.0, _FadeInDuration, T);

                #endif

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
