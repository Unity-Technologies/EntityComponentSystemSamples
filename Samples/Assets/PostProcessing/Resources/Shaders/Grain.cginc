#ifndef __GRAIN__
#define __GRAIN__

#include "Common.cginc"
#include "ColorGrading.cginc"

//
// Adapted & optimized from Film Grain post-process shader v1.1
// Martins Upitis(martinsh) devlog - martinsh.blogspot.com 2013
//
// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
// So you are free to share, modify and adapt it for your needs, and even use it for commercial use.
//

float3 Rnm(half2 tc, half time)
{
    float noise = sin(dot(tc + time.xx, float2(12.9898, 78.233))) * 43758.5453;
    return frac(noise.xxx * float3(1.0, 1.2154, 1.3647)) * 2.0 - 1.0;
}

inline half Fade(half t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// 2d gradient noise
half PNoise(half2 p, half time)
{
    const half kPermTexUnit = 1.0 / 256.0;
    const half kPermTexUnitHalf = 0.5 / 256.0;

    half2 pi = kPermTexUnit * floor(p) + kPermTexUnitHalf;
    half2 pf = frac(p);

    half perm00 = Rnm(pi, time).z;
    half2 grad000 = Rnm(half2(perm00, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
    half n000 = dot(grad000, pf);

    half perm01 = Rnm(pi + half2(0.0, kPermTexUnit), time).z;
    half2 grad010 = Rnm(half2(perm01, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
    half n010 = dot(grad010, pf - half2(0.0, 1.0));

    half perm10 = Rnm(pi + half2(kPermTexUnit, 0.0), time).z;
    half2 grad100 = Rnm(half2(perm10, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
    half n100 = dot(grad100, pf - half2(1.0, 0.0));

    half perm11 = Rnm(pi + half2(kPermTexUnit, kPermTexUnit), time).z;
    half2 grad110 = Rnm(half2(perm11, kPermTexUnitHalf), time).xy * 4.0 - 1.0;
    half n110 = dot(grad110, pf - half2(1.0, 1.0));

    half2 n_x = lerp(half2(n000, n010), half2(n100, n110), Fade(pf.x));

    return lerp(n_x.x, n_x.y, Fade(pf.y));
}

half2 CoordRot(half2 tc, half2 angle, half aspect)
{
    half s = angle.y;
    half c = angle.x;
    tc = tc * 2.0 - 1.0;
    half rotX = (tc.x * aspect * c) - (tc.y * s);
    half rotY = (tc.y * c) + (tc.x * aspect * s);
    rotX = (rotX / aspect) * 0.5 + 0.5;
    rotY = rotY * 0.5 + 0.5;
    return half2(rotX, rotY);
}

float FNoise(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453) * 2.0 - 1.0;
}

//
// params1 = (amount, size, lum_contrib, aspect)
// params2 = (cos_angle, sin_angle)
//
half3 ApplyGrain(half3 color, half2 uv, half4 params1, half3 params2)
{
    half amount = params1.x;
    half size = params1.y;
    half lum_contrib = params1.z;
    half aspect = params1.w;
    half2 angle = params2.xy;
    half time = params2.z;

#if GRAIN_FAST
    half n = FNoise(uv + angle);
#else
    half2 rotCoordsR = CoordRot(uv, angle, aspect);
    half n = PNoise(rotCoordsR * _ScreenParams.xy / size, time);
#endif

    // Noisiness response curve based on scene luminance
    half luminance = lerp(0.0, AcesLuminance(color), lum_contrib);
    half lum = smoothstep(0.2, 0.0, luminance) + luminance;

    n = lerp(n, 0.0, Pow4(lum));
    color = color + n.xxx * amount;

    return color;
}

#endif // __GRAIN__
