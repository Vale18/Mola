//Stylized Water 2
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_FoamTex);
SAMPLER(sampler_FoamTex);

float SampleFoamTexture(float2 uv, float tiling, float subTiling, float2 time, float speed, float subSpeed, float slopeMask, float slopeSpeed, bool slopeFoam)
{
	float4 uvs = PackedUV(uv * tiling, time, speed, subTiling, subSpeed);

	float f1 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.xy).r;	
	float f2 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.zw).r;

	#if UNITY_COLORSPACE_GAMMA
	f1 = SRGBToLinear(f1);
	f2 = SRGBToLinear(f2);
	#endif

	float foam = saturate(f1 + f2);

	if(slopeFoam)
	{
		uvs = PackedUV(uv * tiling, time, speed * slopeSpeed, subTiling, subSpeed * slopeSpeed);
		//Stretch UV vertically on slope
		uvs.yw *= 1-_SlopeStretching;

		//Cannot reuse the same UV, slope foam needs to be resampled and blended in
		float f3 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.xy).r;
		float f4 = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uvs.zw).r;

		#if UNITY_COLORSPACE_GAMMA
		f3 = SRGBToLinear(f3);
		f4 = SRGBToLinear(f4);
		#endif

		half slopeFoam = saturate(f3 + f4);
	
		foam = lerp(foam, slopeFoam, slopeMask);
	}

	return foam;
}