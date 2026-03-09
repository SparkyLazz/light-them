Shader "Custom/SoulEffect"
{
    Properties
    {
        _MainTex                ("Sprite Texture (white square)", 2D) = "white" {}

        [Header(Soul Colors)]
        _CoreColorPure          ("Core Color (full sanity)",    Color)        = (1.0, 0.95, 0.6, 1)
        _EdgeColorPure          ("Edge Color (full sanity)",    Color)        = (1.0, 0.4, 0.05, 1)
        _CoreColorCorrupt       ("Core Color (zero sanity)",    Color)        = (0.7, 0.0, 1.0, 1)
        _EdgeColorCorrupt       ("Edge Color (zero sanity)",    Color)        = (0.1, 0.0, 0.25, 1)

        [Header(Soul Shape)]
        _BodyRadius             ("Body Radius (round bottom)",  Range(0.1, 0.6))  = 0.35
        _BodyYOffset            ("Body Center Y",               Range(-0.5, 0.0)) = -0.15
        _TipHeight              ("Tip Height",                  Range(0.1, 0.8))  = 0.45
        _TipSharpness           ("Tip Sharpness",               Range(1, 5))      = 2.0

        [Header(Wispy Edge)]
        _WispNoiseScale         ("Wisp Noise Scale",            Range(1, 8))      = 3.5
        _WispSpeed              ("Wisp Rise Speed",             Range(0, 4))      = 1.4
        _WispStrength           ("Wisp Erosion Strength",       Range(0, 1))      = 0.45
        _WispEdgeSoftness       ("Wisp Edge Softness",          Range(0.01, 0.3)) = 0.12

        [Header(Inner Flame)]
        _FlameSpeed             ("Flame Flicker Speed",         Range(0, 6))      = 0.8
        _FlameScale             ("Flame Noise Scale",           Range(1, 6))      = 2.5
        _FlameDistort           ("Flame Distortion",            Range(0, 0.3))    = 0.08

        [Header(Glitch)]
        _GlitchIntensity        ("Glitch Intensity",            Range(0, 1))      = 0.6
        _GlitchSpeed            ("Glitch Speed",                Range(0, 20))     = 8.0
        _GlitchBlockSize        ("Glitch Block Size",           Range(0.01, 0.3)) = 0.08
        _ChromaSplit            ("Chroma Split",                Range(0, 0.3))    = 0.15

        [Header(Spawn)]
        _SpawnDuration          ("Spawn Duration (sec)",        Range(0.1, 4))    = 1.5
        _SpawnScatterScale      ("Scatter Noise Scale",         Range(1, 10))     = 5.0

        [Header(Runtime)]
        _Sanity                 ("Sanity (0=corrupt 1=pure)",   Range(0, 1))      = 1.0
        _SpawnTime              ("Spawn Time (set by controller)", Float)         = -999
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SoulEffect"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColorPure;
                float4 _EdgeColorPure;
                float4 _CoreColorCorrupt;
                float4 _EdgeColorCorrupt;
                float  _BodyRadius;
                float  _BodyYOffset;
                float  _TipHeight;
                float  _TipSharpness;
                float  _WispNoiseScale;
                float  _WispSpeed;
                float  _WispStrength;
                float  _WispEdgeSoftness;
                float  _FlameSpeed;
                float  _FlameScale;
                float  _FlameDistort;
                float  _GlitchIntensity;
                float  _GlitchSpeed;
                float  _GlitchBlockSize;
                float  _ChromaSplit;
                float  _SpawnDuration;
                float  _SpawnScatterScale;
                float  _Sanity;
                float  _SpawnTime;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            // ── Noise ─────────────────────────────────────────────
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Hash1(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash(i),                Hash(i + float2(1, 0)), u.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x),
                    u.y);
            }

            float FBM(float2 p)
            {
                float v  = 0.0;
                v += 0.50 * ValueNoise(p);
                v += 0.30 * ValueNoise(p * 2.1  + float2(1.7, 9.2));
                v += 0.12 * ValueNoise(p * 4.3  + float2(8.3, 2.8));
                v += 0.08 * ValueNoise(p * 8.7  + float2(4.1, 6.5));
                return v;
            }

            // ── Glitch ────────────────────────────────────────────
            float2 GlitchOffset(float2 uv, float corrupt, float T)
            {
                float gTime     = floor(T * _GlitchSpeed);
                float trigger   = Hash1(gTime * 7.319);
                float threshold = 1.0 - corrupt * _GlitchIntensity;
                if (trigger > threshold) return float2(0, 0);

                float row      = floor(uv.y / _GlitchBlockSize);
                float rowRand  = Hash1(row * 432.1 + gTime * 13.7);
                float rowShift = step(0.5, rowRand);
                float offsetX  = (rowRand - 0.5) * 0.25 * corrupt * rowShift;
                return float2(offsetX, 0.0);
            }

            // ── Soul shape ────────────────────────────────────────
            float SoulShape(float2 uv)
            {
                float2 p = (uv - 0.5) * 2.0;

                float2 bodyCenter = float2(0.0, _BodyYOffset);
                float  bodyDist   = _BodyRadius - length(p - bodyCenter);

                float tipT     = saturate((p.y - (_BodyYOffset + _BodyRadius * 0.6)) / _TipHeight);
                float tipWidth = _BodyRadius * 0.9 * pow(1.0 - tipT, _TipSharpness);
                float tipDist  = tipWidth - abs(p.x);
                float tipMask  = step(_BodyYOffset, p.y);
                float tip      = tipDist * tipMask;

                return max(bodyDist, tip);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv      = IN.uv;
                float  T       = _Time.y;
                float  sanity  = saturate(_Sanity);
                float  corrupt = 1.0 - sanity;

                // ── Spawn progress ────────────────────────────────
                // spawnT: 0 = just spawned (scattered wisps), 1 = fully formed
                float elapsed = T - _SpawnTime;
                float spawnT  = saturate(elapsed / _SpawnDuration);
                // Smooth ease-in curve so formation accelerates at the end
                float spawnEase = spawnT * spawnT * (3.0 - 2.0 * spawnT);

                // ── Scatter noise (wisp particles before form) ────
                // Fast-moving noise that acts as scattered pre-form wisps
                float2 scatterUV = uv * _SpawnScatterScale + float2(T * 0.6, T * 0.4);
                float  scatter   = FBM(scatterUV);
                // Threshold rises as spawn progresses — wisps coalesce into shape
                float  scatterThresh = lerp(0.65, 0.0, spawnEase);
                float  scatterAlpha  = smoothstep(scatterThresh, scatterThresh + 0.15, scatter)
                                     * (1.0 - spawnEase); // fades out as shape forms

                // ── Spawn erosion: shape dissolves in from heavy erosion ──
                // At spawnT=0 the shape is fully eroded (invisible)
                // At spawnT=1 erosion is back to normal
                float spawnErosionBoost = lerp(2.5, 0.0, spawnEase);

                // ── Glitch UV shift ───────────────────────────────
                float2 glitchOff  = GlitchOffset(uv, corrupt, T);
                float2 glitchedUV = uv + glitchOff;

                // ── Flame distortion ──────────────────────────────
                float2 dUV = glitchedUV * _FlameScale + float2(0.0, -T * _FlameSpeed * 0.4);
                float  dnx = FBM(dUV)                     - 0.5;
                float  dny = FBM(dUV + float2(3.2, 1.7)) - 0.5;
                float2 distortedUV = glitchedUV + float2(dnx, dny) * _FlameDistort;

                // ── Soul silhouette ───────────────────────────────
                float shape = SoulShape(distortedUV);

                // ── Wispy edge erosion (spawn erosion added on top) ──
                float2 wispUV  = uv * _WispNoiseScale + float2(0.0, -T * _WispSpeed);
                float  wisp    = FBM(wispUV) - 0.5;
                float  erosion = (_WispStrength + spawnErosionBoost) * (1.0 + corrupt * 1.2);
                float  eroded  = shape + wisp * erosion;
                float  alpha   = smoothstep(0.0, _WispEdgeSoftness, eroded);

                // Add scatter particles on top during spawn
                alpha = saturate(alpha + scatterAlpha);

                clip(alpha - 0.01);

                // ── Core brightness gradient ──────────────────────
                float2 innerUV    = uv * _FlameScale * 0.6 + float2(0.0, -T * _FlameSpeed);
                float  innerN     = FBM(innerUV + float2(5.1, 3.3));
                float2 coreCenter = float2(0.5, 0.5 + _BodyYOffset * 0.5);
                float  coreDist   = 1.0 - saturate(length(uv - coreCenter) * 2.8);
                float  coreGrad   = saturate(coreDist + innerN * 0.3 - 0.05);

                // ── Chroma split ──────────────────────────────────
                float  chromaAmt   = _ChromaSplit * corrupt * length(glitchOff) * 20.0;
                float2 coreCenter2 = float2(0.5, 0.5 + _BodyYOffset * 0.5);

                float coreGradR = saturate(
                    (1.0 - saturate(length(uv + float2( chromaAmt, 0) - coreCenter2) * 2.8))
                    + innerN * 0.3 - 0.05);
                float coreGradB = saturate(
                    (1.0 - saturate(length(uv + float2(-chromaAmt, 0) - coreCenter2) * 2.8))
                    + innerN * 0.3 - 0.05);

                // ── Color ─────────────────────────────────────────
                float3 coreColor = lerp(_CoreColorCorrupt.rgb, _CoreColorPure.rgb, sanity);
                float3 edgeColor = lerp(_EdgeColorCorrupt.rgb, _EdgeColorPure.rgb, sanity);

                float3 colR = lerp(edgeColor, coreColor, coreGradR);
                float3 colG = lerp(edgeColor, coreColor, coreGrad);
                float3 colB = lerp(edgeColor, coreColor, coreGradB);
                float3 col  = float3(colR.r, colG.g, colB.b);
                col = lerp(col, col * 0.35, corrupt * 0.55);

                // Bright flash at the moment the shape fully coalesces
                // spawnFlash peaks at spawnT=1 then immediately gone
                float spawnFlash = smoothstep(0.85, 1.0, spawnEase)
                                 * smoothstep(1.0, 0.9, spawnEase);
                col += coreColor * spawnFlash * 1.5;

                // ── Flicker ───────────────────────────────────────
                float fSpeed  = _FlameSpeed * (1.0 + corrupt * 2.5);
                float flicker = sin(T * fSpeed) * 0.5
                              + sin(T * fSpeed * 1.618 + 1.1) * 0.35;
                flicker = saturate(flicker * 0.55 + 0.5);
                alpha  *= lerp(1.0, flicker, corrupt * 0.45);

                // ── Blackout flash ────────────────────────────────
                float blackout = step(0.97, Hash1(floor(T * _GlitchSpeed * 0.5) * 3.7));
                alpha *= lerp(1.0, 1.0 - blackout, corrupt);

                return half4(col * IN.color.rgb, alpha * IN.color.a);
            }
            ENDHLSL
        }
    }
}
