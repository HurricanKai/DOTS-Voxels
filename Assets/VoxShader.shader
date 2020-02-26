Shader "Custom/VoxelShader" {
    Properties
     { }
 
     SubShader
     {
         Tags { "RenderType"="Opaque" }
         LOD 100
 
         Pass
         {
             CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag
             #pragma multi_compile_instancing
             #pragma instancing_options procedural:setup nolodfade nolightprobe nolightmap assumeuniformscaling maxcount:512
             #include "UnityCG.cginc"
             #include "Matrix.hlsl"
             #pragma enable_d3d11_debug_symbols
             #pragma target 4.5
 
             struct appdata
             {
                 float4 vertex : POSITION;
                 UNITY_VERTEX_INPUT_INSTANCE_ID
             };
 
             struct v2f
             {
                 float4 vertex : SV_POSITION;
                 UNITY_VERTEX_INPUT_INSTANCE_ID
             };
             
             #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                 StructuredBuffer<float4x4> positionBuffer;
                 // StructuredBuffer<float> scaleBuffer;
                 StructuredBuffer<float4> colorBuffer;
             #endif
             
             void setup()
             {
             #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                 // float3 pos = positionBuffer[unity_InstanceID];
                 // float scale = scaleBuffer[unity_InstanceID];
                  
                  // unity_ObjectToWorld = compose(pos, float4(0, 0, 0, 1), float3(scale, scale, scale));
                  unity_ObjectToWorld = positionBuffer[unity_InstanceID];
             #endif
              }
          
             v2f vert(appdata v)
             {
                 v2f o;
 
                 UNITY_SETUP_INSTANCE_ID(v);
                 UNITY_TRANSFER_INSTANCE_ID(v, o);
                 o.vertex = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, v.vertex));
                 return o;
             }
          
             float4 frag(v2f i) : SV_Target
             {
                 UNITY_SETUP_INSTANCE_ID(i);
                 
 #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                 return colorBuffer[unity_InstanceID];
 #endif
                 return float4(1.0, 1.0, 1.0, 1.0);
             }
             ENDCG
         }
     }
}