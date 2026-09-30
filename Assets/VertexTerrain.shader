Shader "Aether/VertexTerrain" {
 Properties { _Color ("Tint", Color) = (1,1,1,1) }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert
 struct Input { float4 color : COLOR; };
 fixed4 _Color;
 void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.color=v.color; }
 void surf(Input IN, inout SurfaceOutput o) { o.Albedo=IN.color.rgb*_Color.rgb; o.Alpha=1; }
 ENDCG
 }
 Fallback "Diffuse"
}
