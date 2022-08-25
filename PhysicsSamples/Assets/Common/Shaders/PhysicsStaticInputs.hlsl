#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"


// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _BaseColor;
float _TopDown;
float _TextureTilingScale;
float _Cutoff;
float _Smoothness;
float _Metallic;
float _BumpScale;
float _ClearCoatMask;
float _ClearCoatSmoothness;
float _Surface;
CBUFFER_END

// NOTE: Do not ifdef the properties for dots instancing, but ifdef the actual usage.
// Otherwise you might break CPU-side as property constant-buffer offsets change per variant.
// NOTE: Dots instancing is orthogonal to the constant buffer above.
#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
UNITY_DOTS_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
UNITY_DOTS_INSTANCED_PROP(float, _TopDown)
UNITY_DOTS_INSTANCED_PROP(float, _TextureTilingScale)
UNITY_DOTS_INSTANCED_PROP(float, _Cutoff)
UNITY_DOTS_INSTANCED_PROP(float, _Smoothness)
UNITY_DOTS_INSTANCED_PROP(float, _Metallic)
UNITY_DOTS_INSTANCED_PROP(float, _BumpScale)
UNITY_DOTS_INSTANCED_PROP(float, _ClearCoatMask)
UNITY_DOTS_INSTANCED_PROP(float, _ClearCoatSmoothness)
UNITY_DOTS_INSTANCED_PROP(float, _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseMap_ST             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseMap_ST)
#define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
#define _TopDown                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _TopDown)
#define _TextureTilingScale     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _TextureTilingScale)
#define _Cutoff                 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff)
#define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Smoothness)
#define _Metallic               UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Metallic)
#define _BumpScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _BumpScale)
#define _ClearCoatMask          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _ClearCoatMask)
#define _ClearCoatSmoothness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _ClearCoatSmoothness)
#define _Surface                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
#endif
