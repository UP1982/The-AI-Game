using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace Aether {
/// <summary>Runtime PBR factories. Callers own returned materials and destroy them when rebuilding a region.</summary>
public static class PhotorealMaterials {
 public const string VertexShaderName="Aether/PhotorealVertex";
 public static Texture2D Texture(string set,string channel)=>Resources.Load<Texture2D>("Photoreal/"+set+"_"+channel);
 static Material Lit(string name,Color tint,float metal,float smoothness){
  var shader=Shader.Find("HDRP/Lit");if(!shader)throw new InvalidOperationException("HDRP/Lit missing: run PhotorealImport.Prepare before building.");
  var m=new Material(shader){name=name,enableInstancing=true};
  m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smoothness);
  m.SetFloat("_SurfaceType",0);m.SetFloat("_MaterialID",1);m.SetFloat("_DoubleSidedEnable",0);
  m.SetFloat("_ReceivesSSR",1);m.SetFloat("_EnableDecals",1);return m;
 }
 static void ApplyMaps(Material m,string set,float worldScale,float normalStrength=1){
  var diffuse=Texture(set,"Diffuse");var normal=Texture(set,"Normal");var mask=Texture(set,"Mask");
  if(diffuse)m.SetTexture("_BaseColorMap",diffuse);if(normal)m.SetTexture("_NormalMap",normal);if(mask)m.SetTexture("_MaskMap",mask);
  m.SetFloat("_UVBase",5);m.SetFloat("_ObjectSpaceUVMapping",0);m.SetFloat("_TexWorldScale",worldScale);
  m.SetFloat("_NormalMapSpace",0);m.SetFloat("_NormalScale",normalStrength);m.SetFloat("_InvTilingScale",1);
  m.SetVector("_UVMappingMask",Vector4.zero);m.SetFloat("_MetallicRemapMin",0);m.SetFloat("_MetallicRemapMax",1);
  m.SetFloat("_AORemapMin",.35f);m.SetFloat("_AORemapMax",1);m.SetFloat("_SmoothnessRemapMin",.04f);m.SetFloat("_SmoothnessRemapMax",.8f);
 }
 public static Material CreateSurface(Color tint,float metal=0,float emission=0){
  var m=Lit(metal>.35f?"Aether aged bronze":"Aether weathered stone",tint,metal,metal>.35f?.48f:.24f);
  // Bronze and luminous inlays retain their authored colors; neutral masonry gets scanned stone detail.
  float spread=Mathf.Max(tint.r,Mathf.Max(tint.g,tint.b))-Mathf.Min(tint.r,Mathf.Min(tint.g,tint.b));
  if(metal<.35f&&emission<=0&&spread<.24f){ApplyMaps(m,"Stone",.55f,.72f);m.SetColor("_BaseColor",Color.Lerp(Color.white,tint,.62f));}
  if(emission>0){m.SetColor("_EmissiveColor",tint.linear*(emission*2500));m.SetColor("_EmissiveColorLDR",tint);m.SetFloat("_UseEmissiveIntensity",0);m.SetFloat("_EmissiveExposureWeight",1);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
  HDMaterial.ValidateMaterial(m);return m;
 }
 public static Material CreateGround(string biome){
  string set=biome=="desert"?"Sand":biome=="tundra"||biome=="frozen"||biome=="ice"?"Snow":"GroundForest";
  var m=Lit("Aether "+set,Color.white,0,.24f);ApplyMaps(m,set,set=="Sand"?.22f:.32f,.85f);HDMaterial.ValidateMaterial(m);return m;
 }
 public static Material CreateWood(Color tint){
  var m=Lit("Aether textured bark",Color.Lerp(Color.white,tint,.45f),0,.22f);ApplyMaps(m,"Bark",.65f,.8f);HDMaterial.ValidateMaterial(m);return m;
 }
 public static Material CreateLeaves(Color tint){
  var m=Lit("Aether leaf surface",tint,0,.31f);m.SetFloat("_DoubleSidedEnable",1);m.SetFloat("_DoubleSidedNormalMode",0);m.doubleSidedGI=true;HDMaterial.ValidateMaterial(m);return m;
 }
 public static Material CreateVertexSurface()=>CreateVertexSurface(Color.white);
 public static Material CreateVertexSurface(Color tint,string textureSet=null,float worldScale=.35f){
  var template=Resources.Load<Material>("PhotorealVertex");
  var shader=template?template.shader:Shader.Find(VertexShaderName);
  if(!shader)throw new InvalidOperationException("Aether vertex-color HDRP graph missing. Run PhotorealImport.Prepare before building.");
  var m=template?new Material(template):new Material(shader);m.name="Aether vertex-color PBR";m.enableInstancing=true;
  m.SetColor("_Tint",tint);m.SetFloat("_Smoothness",.26f);m.SetFloat("_Metallic",0);m.SetFloat("_WorldScale",worldScale);
  if(!string.IsNullOrEmpty(textureSet)){var diffuse=Texture(textureSet,"Diffuse");if(diffuse)m.SetTexture("_AlbedoMap",diffuse);}
  HDMaterial.ValidateMaterial(m);return m;
 }
}
}
