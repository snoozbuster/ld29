float4x4 xCamerasViewProjection;
float4x4 xLightsViewProjection;
float4x4 xWorld;
float3 xLightPos;
float xLightPower;
float xAmbient;
float3 xColor;
float3 xLightDir;

float4 xClipPlane;
bool xEnableClipping;

bool xEnableCustomAlpha;
float xCustomAlpha;
bool xPassThroughLighting;

//float3 xModelPos;
//float3 xCamPos;
//float xFarPlane;

//Texture xShadowMap;
//Texture xCarLightTexture;
Texture xNormalTexture;
texture2D Texture;

//sampler CarLightSampler = 
//	sampler_state 
//	{ 
//		texture = <xCarLightTexture>; 
//		magfilter = LINEAR; 
//		minfilter = LINEAR; 
//		mipfilter = LINEAR; 
//		AddressU = clamp; 
//		AddressV = clamp;
//	};
//sampler ShadowMapSampler = 
//	sampler_state 
//	{ 
//		texture = <xShadowMap>; 
//		magfilter = LINEAR; 
//		minfilter = LINEAR; 
//		mipfilter = LINEAR; 
//		AddressU = clamp; 
//		AddressV = clamp;
//	};

sampler TextureSampler = 
	sampler_state 
	{ 
		texture = <Texture>; 
		minfilter = Point; magfilter = Point; mipfilter=None;
		AddressU = mirror; 
		AddressV = mirror;
	};

sampler NormalSampler = 
	sampler_state 
	{ 
		texture = <xNormalTexture>; 
		 minfilter = Point; magfilter = Point; mipfilter=None;
		AddressU = mirror; 
		AddressV = mirror;
	};


float DotProduct(float3 lightPos, float3 pos3D, float3 normal)
{
    float3 lightDir = normalize(lightPos - pos3D);
    return dot(normal, lightDir);    
}

/////////////////////////////////
// TECHNIQUE SHADOWEDSCENE
/////////////////////////////////

struct SSceneVertexToPixel
{
    float4 Position             : POSITION;
    float4 Pos2DAsSeenByLight    : TEXCOORD0;

    float2 TexCoords            : TEXCOORD1;
    float4 Position3D            : TEXCOORD2;
	float4 ClipDistances		 : TEXCOORD3;
};

struct SScenePixelToFrame
{
    float4 Color : COLOR0;
};

SSceneVertexToPixel ShadowedSceneVertexShader(float4 inPos : POSITION, float2 inTexCoords : TEXCOORD0)
{
    SSceneVertexToPixel Output = (SSceneVertexToPixel)0;
    
	//Output.ClipDistances.yzw = 0;
	Output.ClipDistances = dot(inPos, xClipPlane);

    float4x4 preWorldViewProjection = mul (xWorld, xCamerasViewProjection);
    float4x4 preLightsWorldViewProjection = mul (xWorld, xLightsViewProjection);

    Output.Position = mul(inPos, preWorldViewProjection);    
    Output.Pos2DAsSeenByLight = mul(inPos, preLightsWorldViewProjection);    
    //Output.Normal = normalize(mul(inNormal, (float3x3)xWorld));    
    Output.Position3D = mul(inPos, xWorld);
    Output.TexCoords = inTexCoords;    

    return Output;
}

SScenePixelToFrame ShadowedScenePixelShader(SSceneVertexToPixel PSIn)
{
	 if(xEnableClipping)
		 clip(PSIn.ClipDistances);
     SScenePixelToFrame Output = (SScenePixelToFrame)0;    

	 float4 baseColor = tex2D(TextureSampler, PSIn.TexCoords);
	
	 if(xPassThroughLighting)
		{
		    Output.Color = baseColor;
			return Output;
		}

	 //if(xModelPos.r != 0 && xModelPos.g != 0 && xModelPos.b != 0)
	 //{
	 //   float distance = sqrt(pow(xModelPos.r - xCamPos.r, 2) + pow(xModelPos.g - xCamPos.g, 2) + pow(xModelPos.b - xCamPos.b, 2));
	 //	  if(distance > xFarPlane)
	 //	  {
	 //		 Output.Color.a = 0;
	 //		 return Output;
	 //	  }
	 //}

     //float2 ProjectedTexCoords;
     //ProjectedTexCoords[0] = PSIn.Pos2DAsSeenByLight.x/PSIn.Pos2DAsSeenByLight.w/2.0f +0.5f;
     //ProjectedTexCoords[1] = -PSIn.Pos2DAsSeenByLight.y/PSIn.Pos2DAsSeenByLight.w/2.0f +0.5f;
     
     //float diffuseLightingFactor = 0;
     //if ((saturate(ProjectedTexCoords).x == ProjectedTexCoords.x) && (saturate(ProjectedTexCoords).y == ProjectedTexCoords.y))
     //{
     //    float depthStoredInShadowMap = tex2D(ShadowMapSampler, ProjectedTexCoords).r;
     //    float realDistance = PSIn.Pos2DAsSeenByLight.z/PSIn.Pos2DAsSeenByLight.w;
     //    if ((realDistance - 1.0f/100.0f) <= depthStoredInShadowMap)
     //    {
			 // wrap lighting
     //        diffuseLightingFactor = DotProduct(xLightPos, PSIn.Position3D, PSIn.Normal) * 0.5 + 0.5;
             //diffuseLightingFactor = saturate(diffuseLightingFactor);
     //        diffuseLightingFactor *= xLightPower;    
			 
     //        diffuseLightingFactor *= tex2D(CarLightSampler, ProjectedTexCoords).r;        
     //    }
     //}
         
     //float4 baseColor = tex2D(TextureSampler, PSIn.TexCoords);                
	 //Output.Color = baseColor*(diffuseLightingFactor + xAmbient);
     //Output.Color = baseColor*(diffuseLightingFactor + xAmbient) * float4(xColor, 1); // makes everything very red

     // Wrap lighting with lighting coming straight down (assuming Z is up).

	 float3 Normal = tex2D(NormalSampler, PSIn.TexCoords) * 2.0f - 1;

	 float diffuseLightingFactor = DotProduct(xLightPos, PSIn.Position3D, Normal) * 0.5 + 0.5;
	 diffuseLightingFactor *= xLightPower;

	 Output.Color.rgb = baseColor * (diffuseLightingFactor + xAmbient);// * xColor;
	
     Output.Color.a = baseColor.a;
	 if(xEnableCustomAlpha)
	 {
		Output.Color.a = xCustomAlpha;
		Output.Color *= xCustomAlpha;
	 }

     return Output;
}

technique ShadowedScene
{
    pass Pass0
    {
        VertexShader = compile vs_2_0 ShadowedSceneVertexShader();
        PixelShader = compile ps_2_0 ShadowedScenePixelShader();
    }
}

/////////////////////////////////
// END SHADOWEDSCENE
/////////////////////////////////

void VS_Ortho(in float4 inPos : POSITION, in float2 TexCoords : TEXCOORD0, out float4 outPos : POSITION, out float2 TexOut : TEXCOORD0)
{
	float4 temp = mul(inPos, xWorld);
	outPos = mul(temp, xCamerasViewProjection);
	TexOut = TexCoords;
}

float4 PS_Ortho(in float2 TexCoords : TEXCOORD0) : COLOR0
{
	 return tex2D(TextureSampler, TexCoords);
}

technique OrthographicProjection
{
	pass Pass0
	{
		VertexShader = compile vs_2_0 VS_Ortho();
		PixelShader = compile ps_2_0 PS_Ortho();
	}
}