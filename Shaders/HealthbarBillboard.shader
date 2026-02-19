// Upgrade NOTE: upgraded instancing buffer 'MyroPHealthbarBillboard' to new syntax.

// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MyroP/HealthbarBillboard"
{
	Properties
	{
		_BorderColor("BorderColor", Color) = (1,1,1,0)
		_FullHealthColor("FullHealthColor", Color) = (0.1201495,0.5660378,0.1469726,0)
		_LowHealthColor("LowHealthColor", Color) = (0.6603774,0.1027946,0.1027946,0)
		_BackgroundColor("BackgroundColor", Color) = (0,0,0,0)
		_BorderX("BorderX", Range( 0 , 0.5)) = 0.07413043
		_BorderY("BorderY", Range( 0 , 0.5)) = 0.07329128
		_Health("Health", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#pragma target 3.5
		#pragma multi_compile_instancing
		#define ASE_VERSION 19801
		#pragma surface surf StandardCustomLighting keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
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

		uniform float4 _LowHealthColor;
		uniform float4 _FullHealthColor;
		uniform float4 _BackgroundColor;
		uniform float4 _BorderColor;
		uniform float _BorderX;
		uniform float _BorderY;

		UNITY_INSTANCING_BUFFER_START(MyroPHealthbarBillboard)
			UNITY_DEFINE_INSTANCED_PROP(float, _Health)
#define _Health_arr MyroPHealthbarBillboard
		UNITY_INSTANCING_BUFFER_END(MyroPHealthbarBillboard)

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			//Calculate new billboard vertex position and normal;
			float3 upCamVec = normalize ( UNITY_MATRIX_V._m10_m11_m12 );
			float3 forwardCamVec = -normalize ( UNITY_MATRIX_V._m20_m21_m22 );
			float3 rightCamVec = normalize( UNITY_MATRIX_V._m00_m01_m02 );
			float4x4 rotationCamMatrix = float4x4( rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1 );
			v.normal = normalize( mul( float4( v.normal , 0 ), rotationCamMatrix )).xyz;
			v.tangent.xyz = normalize( mul( float4( v.tangent.xyz , 0 ), rotationCamMatrix )).xyz;
			//This unfortunately must be made to take non-uniform scaling into account;
			//Transform to world coords, apply rotation and transform back to local;
			v.vertex = mul( v.vertex , unity_ObjectToWorld );
			v.vertex = mul( v.vertex , rotationCamMatrix );
			v.vertex = mul( v.vertex , unity_WorldToObject );
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			c.rgb = 0;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			float _Health_Instance = UNITY_ACCESS_INSTANCED_PROP(_Health_arr, _Health);
			float3 lerpResult28 = lerp( _LowHealthColor.rgb , _FullHealthColor.rgb , _Health_Instance);
			float2 appendResult19 = (float2(_BorderX , _BorderY));
			float2 break22 = floor( ( abs( ( i.uv_texcoord - float2( 0.5,0.5 ) ) ) + float2( 0.5,0.5 ) + appendResult19 ) );
			float border25 = saturate( ( break22.x + break22.y ) );
			float4 lerpResult24 = lerp( _BackgroundColor , _BorderColor , border25);
			float4 lerpResult38 = lerp( float4( ( lerpResult28 * saturate( floor( ( _Health_Instance + 1.0 + -i.uv_texcoord.x ) ) ) ) , 0.0 ) , lerpResult24 , border25);
			o.Emission = lerpResult38.rgb;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.CommentaryNode;23;-2128,-848;Inherit;False;1766.973;458.8104;Comment;12;20;21;22;13;12;19;16;18;8;7;6;25;Border generator;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;6;-2080,-800;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;7;-1808,-800;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-2080,-544;Inherit;False;Property;_BorderY;BorderY;5;0;Create;True;0;0;0;False;0;False;0.07329128;0;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-2080,-624;Inherit;False;Property;_BorderX;BorderX;4;0;Create;True;0;0;0;False;0;False;0.07413043;0;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;8;-1600,-800;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-1760,-608;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;12;-1456,-800;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FloorOpNode;13;-1296,-800;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;22;-1136,-800;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-2048,720;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-976,-800;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;36;-1792,752;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-1936,464;Inherit;False;InstancedProperty;_Health;Health;6;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;20;-800,-800;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;32;-1568,704;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;25;-624,-800;Inherit;False;border;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;3;-1872,32;Inherit;False;Property;_LowHealthColor;LowHealthColor;2;0;Create;True;0;0;0;False;0;False;0.6603774,0.1027946,0.1027946,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;2;-1872,272;Inherit;False;Property;_FullHealthColor;FullHealthColor;1;0;Create;True;0;0;0;False;0;False;0.1201495,0.5660378,0.1469726,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FloorOpNode;35;-1344,704;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;28;-1584,416;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;34;-1216,704;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-1504,976;Inherit;False;Property;_BackgroundColor;BackgroundColor;3;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;1;-1504,1184;Inherit;False;Property;_BorderColor;BorderColor;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode;26;-1440,1424;Inherit;False;25;border;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-960,576;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;39;-768,1136;Inherit;False;25;border;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;24;-1104,1184;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;38;-560,848;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-96,800;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;CustomLighting;MyroP/HealthbarBillboard;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;True;Spherical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;7;0;6;0
WireConnection;8;0;7;0
WireConnection;19;0;16;0
WireConnection;19;1;18;0
WireConnection;12;0;8;0
WireConnection;12;2;19;0
WireConnection;13;0;12;0
WireConnection;22;0;13;0
WireConnection;21;0;22;0
WireConnection;21;1;22;1
WireConnection;36;0;29;1
WireConnection;20;0;21;0
WireConnection;32;0;27;0
WireConnection;32;2;36;0
WireConnection;25;0;20;0
WireConnection;35;0;32;0
WireConnection;28;0;3;5
WireConnection;28;1;2;5
WireConnection;28;2;27;0
WireConnection;34;0;35;0
WireConnection;37;0;28;0
WireConnection;37;1;34;0
WireConnection;24;0;4;0
WireConnection;24;1;1;0
WireConnection;24;2;26;0
WireConnection;38;0;37;0
WireConnection;38;1;24;0
WireConnection;38;2;39;0
WireConnection;0;2;38;0
ASEEND*/
//CHKSM=37D06771C237AE133D8926C34A58C66DD150878D