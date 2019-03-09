// Upgrade NOTE: upgraded instancing buffer 'FishInstanceProperties' to new syntax.

Shader "Custom/Wiggle" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGBA)", 2D) = "white" {}
		_Gloss ("_MetallicGloss (RGB)", 2D) = "white" {}
		_Tints ("Tints (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Amount ("Wave1 Frequency", float) = 1
		_TimeScale ("Wave1 Speed", float) = 1.0
		_Distance ("Distance", float) = 0.1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows  vertex:vert addshadow
		#pragma target 3.0
		#pragma multi_compile_instancing

		sampler2D _MainTex;
		sampler2D _Tints;
		sampler2D _Gloss;

		struct Input {
			float2 uv_MainTex;
		};

		half4 _Direction;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _TimeScale;
		float _Amount;
		float _Distance;

        UNITY_INSTANCING_BUFFER_START (FishInstanceProperties)
        	UNITY_DEFINE_INSTANCED_PROP (float, _InstanceCycleOffset)
#define _InstanceCycleOffset_arr FishInstanceProperties
        UNITY_INSTANCING_BUFFER_END(FishInstanceProperties)

		void vert(inout appdata_full v)
		{
			float cycleOffset = UNITY_ACCESS_INSTANCED_PROP(_InstanceCycleOffset_arr, _InstanceCycleOffset);
			float4 offs = sin((cycleOffset + _Time.y) * _TimeScale + v.vertex.z * _Amount) * _Distance;
			v.vertex.x += offs;
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 g = tex2D (_Gloss, IN.uv_MainTex);
			fixed4 tintColour = tex2D (_Tints, float2(UNITY_ACCESS_INSTANCED_PROP(_InstanceCycleOffset_arr, _InstanceCycleOffset), 0));
			o.Albedo = lerp(c.rgb, c.rgb * tintColour, c.a) * _Color;
			o.Metallic =  _Metallic;
			o.Smoothness = g.a * _Glossiness;
			//o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
