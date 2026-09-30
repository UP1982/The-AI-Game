using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
namespace Aether {
/// <summary>Build-time importer and original HDRP Shader Graph authoring; no vendor graph is copied.</summary>
public static class PhotorealImport {
 const string GraphPath="Assets/Shaders/PhotorealVertex.shadergraph";
 const BindingFlags Members=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 [MenuItem("Aether/Prepare Photoreal Materials")]
 public static void Prepare(){
  Directory.CreateDirectory("Assets/Shaders");Directory.CreateDirectory("Assets/Resources/Photoreal");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  foreach(var path in Directory.GetFiles("Assets/Resources/Photoreal","*",SearchOption.TopDirectoryOnly)){
   if(AssetImporter.GetAtPath(path) is TextureImporter importer){ConfigureTexture(importer);importer.SaveAndReimport();}
  }
  BuildVertexGraph();
  AssetDatabase.ImportAsset(GraphPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
  if(!shader)throw new InvalidOperationException("HDRP vertex Shader Graph import failed.");
  if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("HDRP vertex Shader Graph has compile errors; inspect shader diagnostics.");
  SaveMaterial("Assets/Resources/PhotorealVertex.mat",new Material(shader){name="Aether HDRP Vertex Color"});
  SaveMaterial("Assets/Resources/PhotorealSurface.mat",PhotorealMaterials.CreateSurface(Color.white));
  // Regions create TerrainData only at runtime. Retain its map variants explicitly; scene scanning
  // otherwise sees no TerrainLayers and can strip the local normal/mask keyword combinations.
  var terrainShader=Shader.Find("HDRP/TerrainLit");if(!terrainShader)throw new InvalidOperationException("HDRP/TerrainLit is missing.");
  var terrain=new Material(terrainShader){name="Aether photographic terrain",enableInstancing=true};
  terrain.SetFloat("_EnableInstancedPerPixelNormal",1);terrain.SetFloat("_EnableHeightBlend",0);
  SaveMaterial("Assets/Resources/PhotorealTerrain.mat",terrain,"_NORMALMAP","_MASKMAP","_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
  // Keep representative shader variants and their maps in the player build.
  SaveMaterial("Assets/Resources/Photoreal/StoneSurface.mat",PhotorealMaterials.CreateSurface(new Color(.7f,.72f,.67f)));
  SaveMaterial("Assets/Resources/Photoreal/BronzeSurface.mat",PhotorealMaterials.CreateSurface(new Color(.68f,.43f,.2f),.8f));
  SaveMaterial("Assets/Resources/Photoreal/GlowSurface.mat",PhotorealMaterials.CreateSurface(new Color(.84f,.64f,.32f),0,1));
  SaveMaterial("Assets/Resources/Photoreal/BarkSurface.mat",PhotorealMaterials.CreateWood(Color.white));
  SaveMaterial("Assets/Resources/Photoreal/LeafSurface.mat",PhotorealMaterials.CreateLeaves(new Color(.23f,.39f,.19f)));
  AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  Debug.Log("AETHER_PHOTOREAL_MATERIALS_PREPARED: original HDRP Lit vertex graph and photographic PBR surfaces.");
 }
 static void SaveMaterial(string path,Material fresh,params string[] retainedKeywords){
  HDMaterial.ValidateMaterial(fresh);foreach(var keyword in retainedKeywords)fresh.EnableKeyword(keyword);var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(existing){EditorUtility.CopySerialized(fresh,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(fresh);}else AssetDatabase.CreateAsset(fresh,path);
 }
 internal static void ConfigureTexture(TextureImporter importer){
  string stem=Path.GetFileNameWithoutExtension(importer.assetPath);
  importer.mipmapEnabled=true;importer.isReadable=false;importer.anisoLevel=8;importer.filterMode=FilterMode.Trilinear;
  importer.maxTextureSize=2048;importer.alphaIsTransparency=false;
  if(stem=="Sky"){
   importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.TextureCube;
   importer.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;importer.sRGBTexture=false;
   importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
  }else{
   importer.textureShape=TextureImporterShape.Texture2D;importer.wrapMode=TextureWrapMode.Repeat;
   importer.textureType=stem.EndsWith("_Normal",StringComparison.Ordinal)?TextureImporterType.NormalMap:TextureImporterType.Default;
   importer.sRGBTexture=stem.EndsWith("_Diffuse",StringComparison.Ordinal);
   importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
   importer.alphaSource=TextureImporterAlphaSource.FromInput;
  }
 }
 // Shader Graph keeps its authoring APIs internal. Reflection below calls the installed package APIs
 // rather than embedding package-version-specific serialized graph JSON or a hand-written HDRP pass.
 static Type SG(string shortName)=>FindType("UnityEditor.ShaderGraph."+shortName,"Unity.ShaderGraph.Editor");
 static Type HD(string shortName)=>FindType("UnityEditor.Rendering.HighDefinition.ShaderGraph."+shortName,"Unity.RenderPipelines.HighDefinition.Editor");
 static Type FindType(string name,string assembly){return Type.GetType(name+", "+assembly,true);}
 static object New(Type type)=>Activator.CreateInstance(type,true);
 static object Read(object target,string name){
  for(var type=target.GetType();type!=null;type=type.BaseType){var property=type.GetProperty(name,Members|BindingFlags.DeclaredOnly);if(property!=null)return property.GetValue(target);var field=type.GetField(name,Members|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}
  throw new MissingMemberException(target.GetType().FullName,name);
 }
 static void Write(object target,string name,object value){
  for(var type=target.GetType();type!=null;type=type.BaseType){var property=type.GetProperty(name,Members|BindingFlags.DeclaredOnly);if(property!=null){property.SetValue(target,value);return;}var field=type.GetField(name,Members|BindingFlags.DeclaredOnly);if(field!=null){field.SetValue(target,value);return;}}
  throw new MissingMemberException(target.GetType().FullName,name);
 }
 static object Call(object target,string name,params object[] args){
  var type=target is Type staticType?staticType:target.GetType();
  foreach(var method in type.GetMethods(Members).Where(m=>m.Name==name&&!m.IsGenericMethodDefinition)){
   var parameters=method.GetParameters();if(parameters.Length!=args.Length)continue;
   bool match=true;for(int i=0;i<args.Length;i++)if(args[i]!=null&&!parameters[i].ParameterType.IsInstanceOfType(args[i]))match=false;
   if(!match)continue;
   try{return method.Invoke(target is Type?null:target,args);}catch(TargetInvocationException e){throw new InvalidOperationException(type.FullName+"."+name+" failed",e.InnerException??e);}
  }
  throw new MissingMethodException(type.FullName,name+" ("+args.Length+" arguments)");
 }
 static object Descriptor(string context,string name)=>SG("BlockFields").GetNestedType(context,Members).GetField(name,Members).GetValue(null);
 static object Node(object graph,string type){var node=New(SG(type));Call(graph,"AddNode",node,false);return node;}
 static object Property(object graph,string type,string name,string reference,object value){
  var property=New(SG("Internal."+type));Write(property,"displayName",name);Write(property,"overrideReferenceName",reference);Write(property,"generatePropertyBlock",true);
  if(value!=null)Write(property,"value",value);if(type=="ColorShaderProperty")Write(property,"isMainColor",true);
  Call(graph,"AddGraphInput",property,-1);var node=Node(graph,"PropertyNode");Write(node,"property",property);return node;
 }
 static void Connect(object graph,object output,int outSlot,object input,int inSlot){Call(graph,"Connect",Call(output,"GetSlotReference",outSlot),Call(input,"GetSlotReference",inSlot));}
 static object Block(object graph,string name){
  var context=Read(graph,"fragmentContext");foreach(var wrapped in (IEnumerable)Read(context,"blocks")){var block=Read(wrapped,"value");if((string)Read(block,"name")=="SurfaceDescription."+name)return block;}
  throw new InvalidOperationException("HDRP graph block missing: "+name);
 }
 static void BuildVertexGraph(){
  var graph=New(SG("GraphData"));Call(graph,"AddContexts");Write(graph,"path","Aether");
  var target=New(HD("HDTarget"));if(!(bool)Call(target,"TrySetActiveSubTarget",HD("HDLitSubTarget")))throw new InvalidOperationException("HDRP Lit subtarget not available.");
  // Thin generated leaves are visible and lit on both sides, while the graph remains opaque/deferred.
  foreach(var wrapped in (IEnumerable)Read(target,"m_Datas")){
   var settings=Read(wrapped,"value");if(settings.GetType().Name=="SystemData"){
    var mode=HD("DoubleSidedMode");Write(settings,"doubleSidedMode",Enum.Parse(mode,"FlippedNormals"));
   }
  }
  var outputs=new[]{Descriptor("VertexDescription","Position"),Descriptor("VertexDescription","Normal"),Descriptor("VertexDescription","Tangent"),Descriptor("SurfaceDescription","BaseColor"),Descriptor("SurfaceDescription","NormalTS"),Descriptor("SurfaceDescription","Metallic"),Descriptor("SurfaceDescription","Smoothness"),Descriptor("SurfaceDescription","Occlusion"),Descriptor("SurfaceDescription","Emission"),Descriptor("SurfaceDescription","Alpha")};
  var targets=Array.CreateInstance(SG("Target"),1);targets.SetValue(target,0);
  var descriptors=Array.CreateInstance(SG("BlockFieldDescriptor"),outputs.Length);for(int i=0;i<outputs.Length;i++)descriptors.SetValue(outputs[i],i);
  Call(graph,"InitializeOutputs",targets,descriptors);
  var category=Call(SG("CategoryData"),"DefaultCategory",(object)null);Call(graph,"AddCategory",category);
  var vertex=Node(graph,"VertexColorNode");var tint=Property(graph,"ColorShaderProperty","Tint","_Tint",Color.white);
  var albedo=Property(graph,"Texture2DShaderProperty","Albedo","_AlbedoMap",null);
  var scale=Property(graph,"Vector1ShaderProperty","World tiling","_WorldScale",.35f);
  var rough=Property(graph,"Vector1ShaderProperty","Smoothness","_Smoothness",.26f);
  var metal=Property(graph,"Vector1ShaderProperty","Metallic","_Metallic",0f);
  var projection=Node(graph,"TriplanarNode");Connect(graph,albedo,0,projection,1);Connect(graph,scale,0,projection,5);
  var tintMultiply=Node(graph,"MultiplyNode");Connect(graph,vertex,0,tintMultiply,0);Connect(graph,tint,0,tintMultiply,1);
  var textureMultiply=Node(graph,"MultiplyNode");Connect(graph,tintMultiply,2,textureMultiply,0);Connect(graph,projection,0,textureMultiply,1);
  Connect(graph,textureMultiply,2,Block(graph,"BaseColor"),0);Connect(graph,rough,0,Block(graph,"Smoothness"),0);Connect(graph,metal,0,Block(graph,"Metallic"),0);
  Call(graph,"ValidateGraph");var json=(string)Call(SG("Serialization.MultiJson"),"Serialize",graph);File.WriteAllText(GraphPath,json);
 }
}
class PhotorealTexturePostprocessor:AssetPostprocessor {
 void OnPreprocessTexture(){if(assetPath.StartsWith("Assets/Resources/Photoreal/",StringComparison.Ordinal))PhotorealImport.ConfigureTexture((TextureImporter)assetImporter);}
}
}
