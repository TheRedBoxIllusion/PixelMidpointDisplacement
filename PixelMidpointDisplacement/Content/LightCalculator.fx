#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


float lightIntensity;
float3 lightColor;
float2 lightPosition;
float2 renderDimensions;



Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float luminosity = lightIntensity / 
	pow(((lightPosition.x - input.TextureCoordinates.x) * renderDimensions.x) * ((lightPosition.x - input.TextureCoordinates.x) * renderDimensions.x) + 
	((lightPosition.y - input.TextureCoordinates.y) * renderDimensions.y) * ((lightPosition.y - input.TextureCoordinates.y) * renderDimensions.y),
    0.5);
    float4 light = float4(lightColor.x * luminosity, lightColor.y * luminosity, lightColor.z * luminosity, 1);
    
    return light * tex2D(SpriteTextureSampler, input.TextureCoordinates);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};