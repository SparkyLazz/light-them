Shader "Custom/2D/CurseEffect"
{
    Properties
    {
        [Header(Corruption)]
        _Desaturation       ("Desaturation",          Range(0, 1))     = 0.85
        _TintColor          ("Corruption Tint",       Color)           = (0.15, 0.0, 0.05, 1)
        _TintStrength       ("Tint Strength",         Range(0, 1))     = 0.35

        [Header(Vignette)]
        _VignetteColor      ("Vignette Color",        Color)           = (0.05, 0.0, 0.02, 1)
        _VignetteRadius     ("Vignette Radius",       Range(0, 1))     = 0.55
        _VignetteSharpness  ("Vignette Sharpness",    Range(1, 8))     = 3.5

        [Header(Chromatic Aberration)]
        _ChromaStrength     ("Chroma Strength",       Range(0, 0.03))  = 0.008

        [Header(Distortion)]
        _DistortionStrength ("Distortion Strength",   Range(0, 0.05))  = 0.012
        _DistortionSpeed    ("Distortion Speed",      Range(0.1, 4))   = 0.4
        _DistortionScale    ("Distortion Scale",      Range(1, 8))     = 2.5

        [Header(Pulse Hallucination)]
        _PulseSpeed         ("Pulse Speed",           Range(0.1, 6))   = 1.8
        _PulseStrength      ("Pulse Strength",        Range(0, 1))     = 0.4
        _PulseScale         ("Pulse Scale",           Range(0.5, 4))   = 1.2

        [Header(Film Grain)]
        _GrainStrength      ("Grain Strength",        Range(0, 0.15))  = 0.045
        _GrainSpeed         ("Grain Speed",           Range(1, 30))    = 15.0

        [Header(Runtime)]
        _Intensity          ("Intensity",             Range(0, 1))     = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Blend Off
        Cull Off

        Pass
        {
            Name "CurseEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Desaturation;
                float4 _TintColor;
                float  _TintStrength;
                float4 _VignetteColor;
                float  _VignetteRadius;
                float  _VignetteSharpness;
                float  _ChromaStrength;
                float  _DistortionStrength;
                float  _DistortionSpeed;
                float  _DistortionScale;
                float  _PulseSpeed;
                float  _PulseStrength;
                float  _PulseScale;
                float  _GrainStrength;
                float  _GrainSpeed;
                float  _Intensity;
            CBUFFER_END

            float Hash1(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash1(i),               Hash1(i + float2(1,0)), u.x),
                    lerp(Hash1(i + float2(0,1)), Hash1(i + float2(1,1)), u.x),
                    u.y);
            }

            float FBM(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 3; k++)
                {
                    v += a * ValueNoise(p);
                    p  *= 2.1;
                    a  *= 0.48;
                }
                return v;
            }

            float3 ApplyDesaturation(float3 col, float amount)
            {
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                return lerp(col, float3(lum, lum, lum), amount);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float  t  = _Time.y;

                // Irregular pulse — two offset sines = uneven heartbeat
                float pulse = sin(t * _PulseSpeed) * 0.5 + 0.5;
                pulse      += sin(t * _PulseSpeed * 1.37 + 1.2) * 0.3;
                pulse       = saturate(pulse * 0.6) * _PulseStrength * _Intensity;

                // FBM domain-warped distortion
                float2 noiseUV = uv * _DistortionScale + float2(t * _DistortionSpeed * 0.3,
                                                                  t * _DistortionSpeed * 0.17);
                float  nx      = FBM(noiseUV);
                float  ny      = FBM(noiseUV + float2(3.7, 1.5));
                float  warpAmt = _DistortionStrength * _Intensity * (1.0 + pulse);
                float2 warpedUV = uv + float2(nx - 0.5, ny - 0.5) * warpAmt;

                // Screen breathe on pulse
                float2 centered = warpedUV - 0.5;
                warpedUV        = centered * (1.0 + pulse * 0.015 * _PulseScale) + 0.5;

                // Chromatic aberration — RGB split, stronger on pulse peaks
                float  chromaAmt = _ChromaStrength * _Intensity * (1.0 + pulse * 0.8);
                float2 dir       = normalize(centered + float2(0.0001, 0.0001));
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUV + dir * chromaAmt).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUV              ).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUV - dir * chromaAmt).b;
                half4 col = half4(r, g, b, 1.0);

                // Desaturate
                col.rgb = ApplyDesaturation(col.rgb, _Desaturation * _Intensity);

                // Dark corruption tint
                col.rgb = lerp(col.rgb, _TintColor.rgb, _TintStrength * _Intensity);

                // Film grain
                float grainT = floor(t * _GrainSpeed);
                float grain  = Hash1(uv * float2(1920.0, 1080.0) + grainT) * 2.0 - 1.0;
                col.rgb     += grain * _GrainStrength * _Intensity;

                // Vignette — breathes with pulse
                float dist    = length(centered) / _VignetteRadius;
                float vignette = saturate(pow(dist, _VignetteSharpness));
                vignette      *= (1.0 + pulse * 0.25);
                col.rgb        = lerp(col.rgb, _VignetteColor.rgb, vignette * _Intensity);

                return col;
            }
            ENDHLSL
        }
    }
}