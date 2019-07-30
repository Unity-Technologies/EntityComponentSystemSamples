Shader "Hidden/Post FX/Temporal Anti-aliasing"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Perspective
        Pass
        {
            CGPROGRAM
                #pragma target 5.0
                #pragma vertex VertSolver
                #pragma fragment FragSolver
                #include "TAA.cginc"
            ENDCG
        }

        // Ortho
        Pass
        {
            CGPROGRAM
                #pragma target 5.0
                #pragma vertex VertSolver
                #pragma fragment FragSolver
                #define TAA_DILATE_MOTION_VECTOR_SAMPLE 0
                #include "TAA.cginc"
            ENDCG
        }

        // MRT Blit
        Pass
        {
            CGPROGRAM
                #pragma target 5.0
                #pragma vertex VertBlit
                #pragma fragment FragBlit
                #include "TAA.cginc"
            ENDCG
        }

        // Alpha Clear
        Pass
        {
            CGPROGRAM
                #pragma target 5.0
                #pragma vertex VertDefault
                #pragma fragment FragAlphaClear
                #include "TAA.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Perspective
        Pass
        {
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex VertSolver
                #pragma fragment FragSolver
                #include "TAA.cginc"
            ENDCG
        }

        // Ortho
        Pass
        {
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex VertSolver
                #pragma fragment FragSolver
                #define TAA_DILATE_MOTION_VECTOR_SAMPLE 0
                #include "TAA.cginc"
            ENDCG
        }

        // MRT Blit
        Pass
        {
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex VertBlit
                #pragma fragment FragBlit
                #include "TAA.cginc"
            ENDCG
        }

        // Alpha Clear
        Pass
        {
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex VertDefault
                #pragma fragment FragAlphaClear
                #include "TAA.cginc"
            ENDCG
        }
    }
}
