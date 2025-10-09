#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};
Texture2D Lightmap;
sampler2D LightmapSampler = sampler_state
{
    Texture = <Lightmap>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 pixelColor = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float4 light = tex2D(LightmapSampler, input.TextureCoordinates);
    if (all(pixelColor == float4(0, 0, 0, 1)) && all(light != float4(0, 0, 0, 0)))
    {
        return light;
    }
    else
    {
        return pixelColor;
    }
    
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};