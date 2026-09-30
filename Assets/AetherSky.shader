Shader "Aether/GradientSky" {
 Properties {
  _Zenith ("Zenith", Color) = (.23,.35,.45,1)
  _Horizon ("Horizon", Color) = (.72,.78,.77,1)
 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION; };
   struct v2f { float4 position:SV_POSITION; float3 direction:TEXCOORD0; };
   half4 _Zenith, _Horizon;
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p) {
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
   }
   v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o; }
   half4 frag(v2f i):SV_Target {
    float3 direction=normalize(i.direction);float h=saturate(direction.y);
    half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.65));
    float2 uv=direction.xz/max(.12,direction.y+.24)*2.8;
    float clouds=noise(uv)*.55+noise(uv*2.7+12)*.28+noise(uv*7.1)*.17;
    float veil=smoothstep(.52,.78,clouds)*smoothstep(.015,.18,h)*(1-smoothstep(.60,.92,h));
    color=lerp(color,lerp(_Horizon.rgb,half3(.95,.94,.88),.55),veil*.42);
    // An infinitely distant circular moon has no sphere perspective distortion or cast shadow.
    float alignment=dot(direction,normalize(float3(-.46,.42,.80)));
    float disk=smoothstep(.99968,.99973,alignment);
    float halo=pow(saturate(alignment),400)*.045;
    color=lerp(color,half3(.87,.88,.79),disk*.8)+halo;
    return half4(color,1);
   }
   ENDCG
  }
 }
}
