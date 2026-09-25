#ifndef GAME_UI_ROUGH_INCLUDED
#define GAME_UI_ROUGH_INCLUDED

sampler2D _OverlapTex;
float4 _MainTex_TexelSize;
float4 _OverlapTex_ST;
float _NoiseIntensity;
float _NoiseScale;
float _EdgeWidth;
float _OverlapScale;
float _OverlapIntensity;
float _OverlapSmooth;
float _OverlapMoveDuration;

float RoughHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float RoughNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(RoughHash(cell), RoughHash(cell + float2(1, 0)), f.x),
                lerp(RoughHash(cell + float2(0, 1)), RoughHash(cell + 1.0), f.x), f.y);
}

float4 RoughSpriteBounds(float4 bounds)
{
    // Plain Images also work without RoughUIImage, using the whole texture.
    if (any(bounds.zw <= bounds.xy))
        bounds = float4(0, 0, 1, 1);

    float2 a = bounds.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    float2 b = bounds.zw * _MainTex_ST.xy + _MainTex_ST.zw;
    return float4(min(a, b), max(a, b));
}

half RoughSampleAlpha(float2 uv, float4 bounds, float2 pixelUV)
{
    float2 halfTexel = min(abs(_MainTex_TexelSize.xy) * 0.5,
                          (bounds.zw - bounds.xy) * 0.5);
    float2 safeUV = clamp(uv, bounds.xy + halfTexel, bounds.zw - halfTexel);
    half alpha = saturate(tex2Dlod(_MainTex, float4(safeUV, 0, 0)).a + _TextureSampleAdd.a);

    // Outside this sprite is transparent, even with Clamp/Repeat or an atlas.
    float2 edge = min(uv - bounds.xy, bounds.zw - uv);
    float2 coverage = saturate(edge / max(pixelUV, float2(0.000001, 0.000001)) + 0.5);
    return alpha * coverage.x * coverage.y;
}

half4 ApplyRoughUI(half4 sprite, float2 uv, float2 localPosition, float4 spriteBounds,
                  float2 overlayPosition, half4 tint, float2 hoverData)
{
    // UV derivatives keep erosion width in screen pixels, independent of texture size.
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float2 pixelUV = abs(dx) + abs(dy);
    float phase = _OverlapMoveDuration > 0.0
        ? _Time.y / max(_OverlapMoveDuration, 0.001) : 0.0;
    float tick = floor(phase);
    float2 offset = tick * float2(0.371, 0.619);
    float noise = saturate(RoughNoise(localPosition * _NoiseScale + offset));
    float radius = max(_EdgeWidth, 0.0) * noise * saturate(_NoiseIntensity);

    if (radius > 0.0001)
    {
        float4 bounds = RoughSpriteBounds(spriteBounds);
        half alpha = sprite.a;
        float2 x = dx * radius;
        float2 y = dy * radius;
        alpha = min(alpha, RoughSampleAlpha(uv + x, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv - x, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv + y, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv - y, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv + (x + y) * 0.70710678, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv - (x + y) * 0.70710678, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv + (x - y) * 0.70710678, bounds, pixelUV));
        alpha = min(alpha, RoughSampleAlpha(uv - (x - y) * 0.70710678, bounds, pixelUV));
        sprite.a = alpha;
    }

    // World XY keeps one continuous pattern across separately positioned UI elements.
    // Only overlay uses continuous time; edge noise always uses the integer tick above.
    float overlapPhase = _OverlapSmooth > 0.5 ? phase : tick;
    float2 overlapOffset = overlapPhase * float2(0.371, 0.619);
    float2 overlapUV = overlayPosition * (0.01 * _OverlapScale);
    overlapUV = overlapUV * _OverlapTex_ST.xy + _OverlapTex_ST.zw + frac(overlapOffset);
    // Repeat even when the texture was imported as a clamped UI sprite.
    // Derivatives must come from unwrapped UVs to avoid mip jumps at tile boundaries.
    half4 overlap = tex2Dgrad(_OverlapTex, frac(overlapUV),
                              ddx(overlapUV), ddy(overlapUV));
    half hoverOnly = step(1.5, hoverData.y);
    half visibility = lerp(1.0, saturate(hoverData.x), hoverOnly);
    half4 color = sprite * tint;
    // Hover reveals the texture over the base color, including black buttons.
    // Ordinary Images retain the original tinted, multiplicative overlay.
    half3 overlayColor = sprite.rgb * overlap.rgb * lerp(tint.rgb, half3(1, 1, 1), hoverOnly);
    color.rgb = lerp(color.rgb, overlayColor,
                     saturate(_OverlapIntensity) * overlap.a * visibility);
    return color;
}

#endif
