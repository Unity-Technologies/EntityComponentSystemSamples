#ifndef __TONEMAPPING__
#define __TONEMAPPING__

#include "ACES.cginc"

// Set to 1 to use the full reference ACES tonemapper. This should only be used for research
// purposes and it's quite heavy and generally overkill.
#define TONEMAPPING_USE_FULL_ACES 0

//
// Neutral tonemapping (Hable/Hejl/Frostbite)
// Input is linear RGB
//
half3 NeutralCurve(half3 x, half a, half b, half c, half d, half e, half f)
{
    return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
}

half3 NeutralTonemap(half3 x, half4 params1, half4 params2)
{
    // ACES supports negative color values and WILL output negative values when coming from ACES or ACEScg
    // Make sure negative channels are clamped to 0.0 as this neutral tonemapper can't deal with them properly
    x = max((0.0).xxx, x);

    // Tonemap
    half a = params1.x;
    half b = params1.y;
    half c = params1.z;
    half d = params1.w;
    half e = params2.x;
    half f = params2.y;
    half whiteLevel = params2.z;
    half whiteClip = params2.w;

    half3 whiteScale = (1.0).xxx / NeutralCurve(whiteLevel, a, b, c, d, e, f);
    x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
    x *= whiteScale;

    // Post-curve white point adjustment
    x /= whiteClip.xxx;

    return x;
}

//
// Filmic tonemapping (pre-exposed ACES approximation, unless TONEMAPPING_USE_FULL_ACES is set to 1)
// Input is ACES2065-1 (AP0 w/ linear encoding)
//
half3 FilmicTonemap(half3 aces)
{
#if TONEMAPPING_USE_FULL_ACES

    half3 oces = RRT(aces * 1.8);
    half3 odt = ODT_RGBMonitor(oces);
    return odt;

#else

    // --- Glow module --- //
    half saturation = rgb_2_saturation(aces);
    half ycIn = rgb_2_yc(aces);
    half s = sigmoid_shaper((saturation - 0.4) / 0.2);
    half addedGlow = 1.0 + glow_fwd(ycIn, RRT_GLOW_GAIN * s, RRT_GLOW_MID);
    aces *= addedGlow;

    // --- Red modifier --- //
    half hue = rgb_2_hue(aces);
    half centeredHue = center_hue(hue, RRT_RED_HUE);
    half hueWeight;
    {
        //hueWeight = cubic_basis_shaper(centeredHue, RRT_RED_WIDTH);
        hueWeight = Pow2(smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / RRT_RED_WIDTH)));
    }

    aces.r += hueWeight * saturation * (RRT_RED_PIVOT - aces.r) * (1.0 - RRT_RED_SCALE);

    // --- ACES to RGB rendering space --- //
    half3 acescg = max(0.0, ACES_to_ACEScg(aces));

    // --- Global desaturation --- //
    //acescg = mul(RRT_SAT_MAT, acescg);
    acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, RRT_SAT_FACTOR.xxx);

    // Quick'n'dirty approximation of the ACES tonemapper - pre-exposed RRT(ODT())
    // Adapted from https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/ to
    // fit our range & exposure.
    const half a = 2.5;
    const half b = 0.03;
    const half c = 2.49;
    const half d = 0.6;
    const half e = 0.2;
    half3 x = acescg;
    half3 color = (x * (a * x + b)) / (x * (c * x + d) + e);

    // Apply gamma adjustment to compensate for dim surround
    color = darkSurround_to_dimSurround(color);

    // Apply desaturation to compensate for luminance difference
    //color = mul(ODT_SAT_MAT, color);
    color = lerp(dot(color, AP1_RGB2Y).xxx, color, ODT_SAT_FACTOR.xxx);

    return ACEScg_to_unity(color);

#endif
}

#endif // __TONEMAPPING__
