Shader "Hidden/Post FX/Depth Of Field"
{
    Properties
    {
        _MainTex ("", 2D) = "black"
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
                #pragma target 3.0
                #include "DepthOfField.cginc"
            ENDCG
        }
    }

    FallBack Off
}
