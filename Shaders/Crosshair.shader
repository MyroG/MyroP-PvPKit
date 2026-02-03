// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MyroP/Crosshair"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Opacity("Opacity", Range( 0 , 2)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Overlay+0" "IgnoreProjector" = "True" }
		Cull Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.5
		#define ASE_VERSION 19801
		struct Input
		{
			float2 uv_texcoord;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _Opacity;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = tex2D( _MainTex, uv_MainTex );
			c.rgb = tex2DNode1.rgb;
			c.a = saturate( ( tex2DNode1.a * _Opacity ) );
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.SamplerNode;1;-304,80;Inherit;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;0;False;0;False;-1;None;70936403484c74043a6adb64fea1dacc;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;20;-288,288;Inherit;False;Property;_Opacity;Opacity;2;0;Create;True;0;0;0;False;0;False;1;1;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;16;-944,48;Inherit;False;356;211;Moving each branch individually;1;2;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;15;-1488,624;Inherit;False;1020.3;437.9;Bigger crosshair when further away;7;25;14;13;9;11;12;10;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;80,208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;3;-1264,80;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4;-1312,208;Inherit;False;Property;_CrosshairThickness;Crosshair Thickness;3;0;Create;True;0;0;0;False;0;False;0;0.183;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1424,336;Inherit;False;Property;_CrosshairPrecision;Crosshair Precision;4;0;Create;True;0;0;0;False;0;False;56.5066;1;1;1000;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;2;-896,96;Inherit;False; float halfT = crosshairThickness * 0.5f@$    float eps   = 0.0001f@$    if (uv.x < 0.5f - crosshairThickness/2.0f && uv.y > 0.5f - crosshairThickness/2.0f && uv.y < 0.5f + crosshairThickness/2.0f) //left$    {$        return float2($            clamp(uv.x * crosshairSize, 0, 0.5f),$            clamp($                (uv.y - 0.5f) * crosshairSize + 0.5f,$                0.5f - halfT + eps,$                0.5f + halfT - eps$            )$        )@$    }$    else if (uv.x > 0.5f + crosshairThickness/2.0f && uv.y > 0.5f - crosshairThickness/2.0f && uv.y < 0.5f + crosshairThickness/2.0f) //right$    {$        return float2($        clamp(1.0f - (1.0f - uv.x) * crosshairSize, 0.5f, 1.0f),$            clamp($                (uv.y - 0.5f) * crosshairSize + 0.5f,$                0.5f - halfT + eps,$                0.5f + halfT - eps$            )$        )@$    }$    else if (uv.y < 0.5f - crosshairThickness/2.0f && uv.x > 0.5f - crosshairThickness/2.0f && uv.x < 0.5f + crosshairThickness/2.0f) //bottom$    {$        return float2($            clamp($                (uv.x - 0.5f) * crosshairSize + 0.5f,$                0.5f - halfT + eps,$                0.5f + halfT - eps$            ),$            clamp(uv.y * crosshairSize, 0.0f, 0.5f)$        )@$    }$    else if (uv.y > 0.5f + crosshairThickness/2.0f && uv.x > 0.5f - crosshairThickness/2.0f && uv.x < 0.5f + crosshairThickness/2.0f) //top$    {$        return float2($            clamp($                (uv.x - 0.5f) * crosshairSize + 0.5f,$                0.5f - halfT + eps,$                0.5f + halfT - eps$            ),$            clamp(1.0f - (1.0f - uv.y) * crosshairSize, 0.5f, 1.0f)$        )@$    }$$    return uv@;2;Create;3;True;uv;FLOAT2;0,0;In;;Inherit;False;True;crosshairThickness;FLOAT;0;In;;Inherit;False;True;crosshairSize;FLOAT;0;In;;Inherit;False;GetCrosshairUV;True;False;0;;False;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-1008,-112;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;17;-576,80;Inherit;False;Property;_Dynamiccrosshair;Dynamic crosshair;6;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ObjectToWorldTransfNode;10;-1376,688;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;12;-1168,672;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceCameraPos;11;-1440,880;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;13;-944,784;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-976,672;Inherit;False;Property;_CrosshairSize;Crosshair Size;5;0;Create;True;0;0;0;False;0;False;1;0.031;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;14;-832,864;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-640,848;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;6;-656,400;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;23;272,208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-304,576;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;28;-48,896;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;31;-960,464;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;512,0;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;CustomLighting;MyroP/Crosshair;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;2;False;;7;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;True;Custom;;Overlay;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Spherical;True;True;Absolute;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;21;0;1;4
WireConnection;21;1;20;0
WireConnection;2;0;3;0
WireConnection;2;1;4;0
WireConnection;2;2;5;0
WireConnection;17;1;19;0
WireConnection;17;0;2;0
WireConnection;12;0;10;1
WireConnection;12;1;10;2
WireConnection;12;2;10;3
WireConnection;13;0;12;0
WireConnection;13;1;10;4
WireConnection;14;0;13;0
WireConnection;14;1;11;0
WireConnection;25;0;9;0
WireConnection;25;1;14;0
WireConnection;23;0;21;0
WireConnection;27;0;6;0
WireConnection;27;1;25;0
WireConnection;31;0;5;0
WireConnection;0;9;23;0
WireConnection;0;13;1;5
ASEEND*/
//CHKSM=275D2C5634995602045823CC842CC945B4AAA830