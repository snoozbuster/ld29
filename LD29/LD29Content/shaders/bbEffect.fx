//------- XNA interface --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xCamPos;
float3 xAllowedRotDir;
float2 dim;

//------- Texture Samplers --------
Texture xBillboardTexture;
sampler textureSampler = sampler_state { texture = <xBillboardTexture> ; minfilter = Point; magfilter = Point; mipfilter=None; AddressU = CLAMP; AddressV = CLAMP;};

struct BBVertexToPixel
{
	float4 Position : POSITION;
	float2 TexCoord	: TEXCOORD0;
	float4 ClipDistances : TEXCOORD1;
};

//------- Technique: CylBillboard --------
BBVertexToPixel CylBillboardVS(float3 inPos: POSITION0, float2 inTexCoord: TEXCOORD0)
{
	BBVertexToPixel Output = (BBVertexToPixel)0;	
	
	float3 center = mul(inPos, xWorld);
	float3 eyeVector = center - xCamPos;	
	
	float3 upVector = xAllowedRotDir;
	upVector = normalize(upVector);
	float3 sideVector = cross(eyeVector,upVector);
	sideVector = normalize(sideVector);
	
	float3 finalPosition = center;
	finalPosition += (inTexCoord.x*dim.x-dim.x/2)*sideVector;
	finalPosition += (dim.y/2-inTexCoord.y*dim.y)*upVector;	
	
	float4 finalPosition4 = float4(finalPosition, 1);
		
	float4x4 preViewProjection = mul (xView, xProjection);
	Output.Position = mul(finalPosition4, preViewProjection);
	
	Output.TexCoord = inTexCoord;
	
	return Output;
}

float4 BillboardPS(BBVertexToPixel PSIn) : COLOR0
{
	return tex2D(textureSampler, PSIn.TexCoord);
}

technique CylBillboard
{
	pass Pass0
    {          
    	VertexShader = compile vs_2_0 CylBillboardVS();
        PixelShader  = compile ps_2_0 BillboardPS();        
    }
}