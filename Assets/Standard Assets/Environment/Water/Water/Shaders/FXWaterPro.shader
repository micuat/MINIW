Shader "FX/Water" {
Properties {
	_WaveScale ("Wave scale", Range (0.02,0.15)) = 0.063
	_ReflDistort ("Reflection distort", Range (0,1.5)) = 0.44
	_RefrDistort ("Refraction distort", Range (0,1.5)) = 0.40
	_RefrColor ("Refraction color", COLOR)  = ( .34, .85, .92, 1)
	[NoScaleOffset] _Fresnel ("Fresnel (A) ", 2D) = "gray" {}
	[NoScaleOffset] _BumpMap ("Normalmap ", 2D) = "bump" {}
	WaveSpeed ("Wave speed (map1 x,y; map2 x,y)", Vector) = (19,9,-16,-7)
	[NoScaleOffset] _ReflectiveColor ("Reflective color (RGB) fresnel (A) ", 2D) = "" {}
	_HorizonColor ("Simple water horizon color", COLOR)  = ( .172, .463, .435, 1)
	[HideInInspector] _ReflectionTex ("Internal Reflection", 2D) = "" {}
	[HideInInspector] _RefractionTex ("Internal Refraction", 2D) = "" {}
    _Center0 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center1 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center2 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center3 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center4 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center5 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center6 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center7 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center8 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center9 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center10 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center11 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center12 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center13 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center14 ("Ripple center", Vector) = (0, 0, 0, 0)
    _Center15 ("Ripple center", Vector) = (0, 0, 0, 0)
}


// -----------------------------------------------------------
// Fragment program cards


Subshader {
	Tags { "WaterMode"="Refractive" "RenderType"="Opaque" }
	Pass {
CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members ripple)
#pragma exclude_renderers d3d11 xbox360
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile WATER_REFRACTIVE WATER_REFLECTIVE WATER_SIMPLE

#if defined (WATER_REFLECTIVE) || defined (WATER_REFRACTIVE)
#define HAS_REFLECTION 1
#endif
#if defined (WATER_REFRACTIVE)
#define HAS_REFRACTION 1
#endif


#include "UnityCG.cginc"

uniform float4 _WaveScale4;
uniform float4 _WaveOffset;

#if HAS_REFLECTION
uniform float _ReflDistort;
#endif
#if HAS_REFRACTION
uniform float _RefrDistort;
#endif

struct appdata {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};

struct v2f {
	float4 pos : SV_POSITION;
    float4 ripple;
	#if defined(HAS_REFLECTION) || defined(HAS_REFRACTION)
		float4 ref : TEXCOORD0;
		float2 bumpuv0 : TEXCOORD1;
		float2 bumpuv1 : TEXCOORD2;
		float3 viewDir : TEXCOORD3;
	#else
		float2 bumpuv0 : TEXCOORD0;
		float2 bumpuv1 : TEXCOORD1;
		float3 viewDir : TEXCOORD2;
	#endif
	UNITY_FOG_COORDS(4)
};

v2f vert(appdata v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	

	// scroll bump waves
	float4 temp;
	float4 wpos = mul (_Object2World, v.vertex);
	temp.xyzw = wpos.xzxz * _WaveScale4 + _WaveOffset;
	o.bumpuv0 = temp.xy;
	o.bumpuv1 = temp.wz;
	
	// object space view direction (will normalize per pixel)
	o.viewDir.xzy = WorldSpaceViewDir(v.vertex);
	
	#if defined(HAS_REFLECTION) || defined(HAS_REFRACTION)
	o.ref = ComputeScreenPos(o.pos);
	#endif

    o.ripple = v.vertex;

    UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

#if defined (WATER_REFLECTIVE) || defined (WATER_REFRACTIVE)
sampler2D _ReflectionTex;
#endif
#if defined (WATER_REFLECTIVE) || defined (WATER_SIMPLE)
sampler2D _ReflectiveColor;
#endif
#if defined (WATER_REFRACTIVE)
sampler2D _Fresnel;
sampler2D _RefractionTex;
uniform float4 _RefrColor;
#endif
#if defined (WATER_SIMPLE)
uniform float4 _HorizonColor;
#endif
sampler2D _BumpMap;
uniform float4 _Center0;
uniform float4 _Center1;
uniform float4 _Center2;
uniform float4 _Center3;
uniform float4 _Center4;
uniform float4 _Center5;
uniform float4 _Center6;
uniform float4 _Center7;
uniform float4 _Center8;
uniform float4 _Center9;
uniform float4 _Center10;
uniform float4 _Center11;
uniform float4 _Center12;
uniform float4 _Center13;
uniform float4 _Center14;
uniform float4 _Center15;

half4 frag( v2f i ) : SV_Target
{
	i.viewDir = normalize(i.viewDir);
	
	// combine two scrolling bumpmaps into one
	half3 bump1 = UnpackNormal(tex2D( _BumpMap, i.bumpuv0 )).rgb;
	half3 bump2 = UnpackNormal(tex2D( _BumpMap, i.bumpuv1 )).rgb;
	half3 bump = (bump1 + bump2) * 0.5;
	
    float4 n;
    half impact = length(i.ripple.xz * 128 - _Center0.xz);
    half tDiff = _Time.y - _Center0.w;
    if( tDiff > 1 )
    tDiff = 1;
    n.x = sin(impact * 32 - _Time.y * 16) * (exp(-impact * 1.2)) * (1 - tDiff);

    impact = length(i.ripple.xz * 128 - _Center1.xz);
    tDiff = _Time.y - _Center1.w;
    if( tDiff > 1 )
    tDiff = 1;
    n.x += sin(impact * 32 - _Time.y * 16) * (exp(-impact * 1.2)) * (1 - tDiff);

    impact = length(i.ripple.xz * 128 - _Center2.xz);
    tDiff = _Time.y - _Center2.w;
    if( tDiff > 1 )
    tDiff = 1;
    n.x += sin(impact * 32 - _Time.y * 16) * (exp(-impact * 1.2)) * (1 - tDiff);

    impact = length(i.ripple.xz * 128 - _Center3.xz);
    tDiff = _Time.y - _Center3.w;
    if( tDiff > 1 )
    tDiff = 1;
    n.x += sin(impact * 32 - _Time.y * 16) * (exp(-impact * 1.2)) * (1 - tDiff);

    n.x = n.x * 0.25f + 0.5f;

    n.y = n.x;
    n.z = n.x;
    n.w = n.x;

    bump += UnpackNormal(n).rgb;

    // fresnel factor
	half fresnelFac = dot( i.viewDir, bump );
	
	// perturb reflection/refraction UVs by bumpmap, and lookup colors
	
	#if HAS_REFLECTION
	float4 uv1 = i.ref; uv1.xy += bump * _ReflDistort;
	half4 refl = tex2Dproj( _ReflectionTex, UNITY_PROJ_COORD(uv1) );
	#endif
	#if HAS_REFRACTION
	float4 uv2 = i.ref; uv2.xy -= bump * _RefrDistort;
	half4 refr = tex2Dproj( _RefractionTex, UNITY_PROJ_COORD(uv2) ) * _RefrColor;
	#endif
	
	// final color is between refracted and reflected based on fresnel
	half4 color;
	
	#if defined(WATER_REFRACTIVE)
	half fresnel = UNITY_SAMPLE_1CHANNEL( _Fresnel, float2(fresnelFac,fresnelFac) );
	color = lerp( refr, refl, fresnel );
	#endif
	
	#if defined(WATER_REFLECTIVE)
	half4 water = tex2D( _ReflectiveColor, float2(fresnelFac,fresnelFac) );
	color.rgb = lerp( water.rgb, refl.rgb, water.a );
	color.a = refl.a * water.a;
	#endif
	
	#if defined(WATER_SIMPLE)
	half4 water = tex2D( _ReflectiveColor, float2(fresnelFac,fresnelFac) );
	color.rgb = lerp( water.rgb, _HorizonColor.rgb, water.a );
	color.a = _HorizonColor.a;
	#endif

	UNITY_APPLY_FOG(i.fogCoord, color);
	return color;
}
ENDCG

	}
}

}
