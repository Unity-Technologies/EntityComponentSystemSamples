// This shader works with URP 7.1.x and above
Shader "Custom/Physics Backface"
{
    Properties
    {
        // Specular vs Metallic workflow
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainColor] _BaseColor("Color", Color) = (0.5,0.5,0.5,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        _ReceiveShadows("Receive Shadows", Float) = 1.0

        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

        SubShader
        {
            Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True"}
            LOD 300

            // ------------------------------------------------------------------
            // Forward pass. Shades GI, fog and all lights in a single pass.
            Pass
            {
                // "Lightmode" tag must be "UniversalForward" or not be defined in order for
                // to render objects.
                Name "StandardBackfaceLit"
                Tags{"LightMode" = "UniversalForward"}

                Blend[_SrcBlend][_DstBlend]
                ZWrite[_ZWrite]
                Cull Front
            
                HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            // unused shader_feature variants are stripped from build automatically
            #pragma shader_feature _ALPHAPREMULTIPLY_ON

            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Render Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"


            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 0);

                float3 positionWS               : TEXCOORD1;
                float3 normalWS                 : TEXCOORD2;
                float2 uv                       : TEXCOORD3;
                float3 viewDirWS                : TEXCOORD4;

                half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord              : TEXCOORD6;
            #endif

                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

            #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
                inputData.positionWS = input.positionWS;
            #endif

                half3 viewDirWS = SafeNormalize(input.viewDirWS);
                inputData.normalWS = input.normalWS;

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = viewDirWS;

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif

                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
            }

            // Used in Standard (Physically Based) shader
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                // normalWS and tangentWS already normalize.
                // this is required to avoid skewing the direction during interpolation
                // also required for per-vertex lighting and SH evaluation
                VertexNormalInputs normalInput = GetVertexNormalInputs(-1 * input.normalOS, input.tangentOS);
                float3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                // already normalized from normal transform to WS.
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = viewDirWS;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                output.positionWS = vertexInput.positionWS;

            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
            #endif

                output.positionCS = vertexInput.positionCS;

                return output;
            }

            // Used in Standard (Physically Based) shader
            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;

                InitializeStandardLitSurfaceData(input.uv, surfaceData);


                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, false);

                return color;
            }
            ENDHLSL
        }

            // Used for rendering shadowmaps
            UsePass "Universal Render Pipeline/Lit/ShadowCaster"

            // Used for depth prepass
            // If shadows cascade are enabled we need to perform a depth prepass. 
            // We also need to use a depth prepass in some cases camera require depth texture
            // (e.g, MSAA is enabled and we can't resolve with Texture2DMS
            UsePass "Universal Render Pipeline/Lit/DepthOnly"

            // Used for Baking GI. This pass is stripped from build.
            //UsePass "Universal Render Pipeline/Lit/Meta"
        }
}
