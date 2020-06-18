Shader "Custom/Physics Backface" 
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        //Tags { "RenderType" = "Opaque" }
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        //Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 200
        Cull Front
        Offset -1, -1

        CGPROGRAM
        #pragma surface surf Standard noshadow alpha:fade
        #pragma target 3.0
        //#pragma surface surf Lambert
        struct Input 
        {
            float4 color : COLOR;
        };
        
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o) 
        {
            fixed4 c = (IN.color * _Color);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Normal = -1 * o.Normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}