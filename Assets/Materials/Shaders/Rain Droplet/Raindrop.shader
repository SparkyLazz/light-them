Shader "Custom/Raindrop" {
    Properties {
        _BlitTexture    ("Source", 2D) = "white" {}
        _RainAmount     ("Rain Amount", Range(0.0, 1.0)) = 0.7
        _Speed          ("Speed", Range(0.0, 3.0)) = 1.0
        _Tint           ("Tint Color", Color) = (1, 1, 1, 1)
        _Opacity        ("Opacity", Range(0.0, 1.0)) = 1.0
        _Refraction     ("Refraction Strength", Range(0.0, 0.05)) = 0.01
        _WindStrength   ("Wind Strength", Range(-1.0, 1.0)) = 0.0

        [KeywordEnum(Low, Medium, High)] _Quality ("Quality Preset", Float) = 1
    }

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass {
            Name "RaindropPass"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma shader_feature_local _QUALITY_LOW _QUALITY_MEDIUM _QUALITY_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half  _RainAmount;
                half  _Speed;
                half4 _Tint;
                half  _Opacity;
                half  _Refraction;
                half  _WindStrength;
            CBUFFER_END

            #define S(a, b, t) smoothstep(a, b, t)

            half N(half t) {
                return frac(sin(t * 12345.564h) * 7658.76h);
            }

            half3 N13(half p) {
                half3 p3 = frac(half3(p,p,p) * half3(.1031h,.11369h,.13787h));
                p3 += dot(p3, p3.yzx + 19.19h);
                return frac(half3((p3.x+p3.y)*p3.z,
                                  (p3.x+p3.z)*p3.y,
                                  (p3.y+p3.z)*p3.x));
            }

            half Saw(half b, half t) {
                return S(0.h, b, t) * S(1.h, b, t);
            }

            half2 DropLayer(half2 uv, half t) {
                half2 UV = uv;

                uv.y += t * 0.75h;
                half2 a    = half2(6.h, 1.h);
                half2 grid = a * 2.h;
                half2 id   = floor(uv * grid);

                half colShift = N(id.x);
                uv.y += colShift;

                id       = floor(uv * grid);
                half3 n  = N13(id.x * 35.2h + id.y * 2376.1h);
                half2 st = frac(uv * grid) - half2(.5h, 0.h);

                half x = n.x - .5h;
                half y = UV.y * 20.h;

                #if defined(_QUALITY_MEDIUM)
                    x += sin(y) * (.5h - abs(x)) * (n.z - .5h) * 0.5h;
                #elif defined(_QUALITY_HIGH)
                    half wiggle = sin(y + sin(y));
                    x += wiggle * (.5h - abs(x)) * (n.z - .5h);
                #endif

                x += _WindStrength * 0.3h * (n.z - 0.5h);
                x *= .7h;

                half ti = frac(t + n.z);
                y = (Saw(.85h, ti) - .5h) * .9h + .5h;
                half2 p = half2(x, y);

                half d        = length((st - p) * a.yx);
                half mainDrop = S(.4h, .0h, d);

                half r  = sqrt(S(1.h, y, st.y));
                half cd = abs(st.x - x);

                half trail      = S(.23h*r, .15h*r*r, cd);
                half trailFront = S(-.02h, .02h, st.y - y);
                trail *= trailFront * r * r;

                half m = mainDrop;

                #if defined(_QUALITY_HIGH)
                    y = UV.y;
                    half trail2   = S(.2h*r, .0h, cd);
                    half droplets = max(0.h, (sin(y*(1.h-y)*120.h) - st.y))
                                    * trail2 * trailFront * n.z;
                    y        = frac(y * 10.h) + (st.y - .5h);
                    half dd  = length(st - half2(x, y));
                    droplets = S(.3h, 0.h, dd);
                    m       += droplets * r * trailFront;
                #endif

                return half2(m, trail);
            }

            half StaticDrops(half2 uv, half t) {
                #if defined(_QUALITY_LOW)
                    uv *= 15.h;
                #elif defined(_QUALITY_MEDIUM)
                    uv *= 20.h;
                #else
                    uv *= 40.h;
                #endif

                half2 id = floor(uv);
                uv       = frac(uv) - .5h;
                half3 n  = N13(id.x * 107.45h + id.y * 3543.654h);
                half2 pp = (n.xy - .5h) * .7h;
                half d   = length(uv - pp);

                half fade = Saw(.025h, frac(t + n.z));
                return S(.3h, 0.h, d) * frac(n.z * 10.h) * fade;
            }

            half2 Drops(half2 uv, half t, half l0, half l1, half l2) {
                half  s  = StaticDrops(uv, t) * l0;
                half2 m1 = DropLayer(uv, t) * l1;

                #if defined(_QUALITY_HIGH)
                    half2 m2 = DropLayer(uv * 1.85h, t) * l2;
                    half c   = S(.3h, 1.h, s + m1.x + m2.x);
                    return half2(c, max(m1.y * l1, m2.y * l2));
                #else
                    half c = S(.3h, 1.h, s + m1.x);
                    return half2(c, m1.y * l1);
                #endif
            }

            half4 Frag(Varyings input) : SV_Target {
                half2 UV = input.texcoord;

                half2 uv = half2(
                    (UV * _ScreenParams.xy - .5h * _ScreenParams.xy) / _ScreenParams.y
                );

                uv.x += uv.y * _WindStrength * 0.4h;

                half t = (half)_Time.y * _Speed * 0.2h;

                half rainAmount  = clamp(_RainAmount, 0.h, 1.h);
                half staticDrops = S(-.5h, 1.h, rainAmount) * 2.h;
                half layer1      = S(.25h, .75h, rainAmount);
                half layer2      = S(.0h,  .5h,  rainAmount);

                half2 c = Drops(uv, t, staticDrops, layer1, layer2);

                half2 e  = half2(.001h, 0.h);
                half  cx = Drops(uv + e, t, staticDrops, layer1, layer2).x;

                #if defined(_QUALITY_LOW)
                    half2 n = half2(cx - c.x, cx - c.x);
                #else
                    half cy = Drops(uv + e.yx, t, staticDrops, layer1, layer2).x;
                    half2 n = half2(cx - c.x, cy - c.x);
                #endif

                #if defined(_QUALITY_LOW)
                    half focus = 3.h;
                #else
                    half focus = lerp(6.h - c.y, 2.h, S(.1h, .2h, c.x));
                #endif

                half2 refractedUV = UV + n * _Refraction * 10.h;
                half3 col = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,
                                                    sampler_LinearClamp,
                                                    refractedUV, focus).rgb;

                half2 vUV = UV - .5h;
                col      *= 1.h - dot(vUV, vUV);
                col      *= _Tint.rgb * _Opacity;

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}