TEXTURE2D(_CausticsTex);
SAMPLER(sampler_CausticsTex);

float3 SampleCaustics(float2 uv, float2 time, float tiling)
{
	float3 caustics1 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv * tiling + (time.xy)).rgb;
	float3 caustics2 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.8) - (time.xy)).rgb;
	
	#if UNITY_COLORSPACE_GAMMA
	caustics1 = SRGBToLinear(caustics1);
	caustics2 = SRGBToLinear(caustics2);
	#endif

	float3 caustics = min(caustics1, caustics2);

	#if HQ_CAUSTICS
	float3 caustics3 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.6) + (time.xy * 1.2)).rgb * 1;
	float3 caustics4 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, (uv * tiling * 0.55) - (time.xy * 1.2)).rgb * 1;
	caustics4 = min(caustics3, caustics4);
	
	caustics = min(caustics, caustics4) * 2.0;
	#endif
	
	#if WAVE_SIMULATION
	SampleWaveCaustics(float3(uv.x, 0, uv.y), caustics);
	#endif
	
	return caustics;
}