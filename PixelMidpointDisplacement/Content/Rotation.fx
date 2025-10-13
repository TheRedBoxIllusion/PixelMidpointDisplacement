#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Matrix WorldViewProjection;
float2 vertex1;
float2 vertex2;
float2 faceDirection;
float2 lightPosition;


float gradient1;
float gradient2;
float gradientBetweenVertexes;
bool flipVertex1;

bool flipVertex2;

bool flippedVertex;

float falloff;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};
Texture2D ShadowTexture;
sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowTexture>;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
	
    output.Position = input.Position;
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
	
    return output;
}

float4 MainPS(VertexShaderOutput input) : SV_TARGET
{
    float4 color = float4(0, 0, 0, 1); //tex2D(SpriteTextureSampler, input.TextureCoordinates);
    //color = tex2D(ShadowSampler, input.TextureCoordinates);

    bool confinedWithinVertex1 = false;

    confinedWithinVertex1 = flipVertex1 ? (input.TextureCoordinates.y - vertex1.y) < gradient1 * (input.TextureCoordinates.x - vertex1.x) : (input.TextureCoordinates.y - vertex1.y) > gradient1 * (input.TextureCoordinates.x - vertex1.x);

    
    bool confinedWithinVertex2 = false;
    confinedWithinVertex2 = flipVertex2 ? (input.TextureCoordinates.y - vertex2.y) > gradient2 * (input.TextureCoordinates.x - vertex2.x) : (input.TextureCoordinates.y - vertex2.y) < gradient2 * (input.TextureCoordinates.x - vertex2.x);


    bool confinedPastFace = flippedVertex ? input.TextureCoordinates.y - vertex1.y > gradientBetweenVertexes * (input.TextureCoordinates.x - vertex1.x) : input.TextureCoordinates.y - vertex1.y < gradientBetweenVertexes * (input.TextureCoordinates.x - vertex1.x);
    if (confinedWithinVertex1 && confinedWithinVertex2 && confinedPastFace)
    {
            color = float4(falloff, falloff, falloff, 1);
    }
    
	return color;
}

technique BasicColorDrawing
{
	pass P0
	{
        
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};