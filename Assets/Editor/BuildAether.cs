using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class BuildAether {
 [MenuItem("Aether/Build macOS")]
 public static void Build(){BuildPlatform(false);}
 [MenuItem("Aether/Build Windows")]
 public static void BuildWindows(){BuildPlatform(true);}
 public static void PrepareGraphics(){Prepare(false);}
 static HDRenderPipelineAsset Pipeline(bool windows){
  Directory.CreateDirectory("Assets/Settings");string path="Assets/Settings/"+(windows?"Aether-Windows-HDRP":"Aether-Mac-HDRP")+".asset";
  var asset=AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);if(!asset){asset=ScriptableObject.CreateInstance<HDRenderPipelineAsset>();AssetDatabase.CreateAsset(asset,path);}
  var settings=asset.currentPlatformRenderPipelineSettings;
  settings.supportSSR=true;settings.supportSSRTransparent=true;settings.supportSSAO=true;settings.supportVolumetrics=true;settings.supportVolumetricClouds=false;settings.supportMotionVectors=true;settings.supportSSGI=false;
  settings.supportedLitShaderMode=RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
  // Unity's DXC compilation of DirectX ray-tracing shaders requires the Windows Editor.
  settings.supportRayTracing=windows&&Application.platform==RuntimePlatform.WindowsEditor;settings.supportedRayTracingMode=RenderPipelineSettings.SupportedRayTracingMode.Performance;
  var shadows=settings.hdShadowInitParams;shadows.supportScreenSpaceShadows=true;settings.hdShadowInitParams=shadows;
  asset.currentPlatformRenderPipelineSettings=settings;EditorUtility.SetDirty(asset);return asset;
 }
 static void Prepare(bool windows){
  Directory.CreateDirectory("Assets/Resources");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Builds");Directory.CreateDirectory("Documentation");
  PlayerSettings.colorSpace=ColorSpace.Linear;QualitySettings.antiAliasing=0;
  var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);graphics.FindProperty("m_InstancingStripping").intValue=2;graphics.ApplyModifiedPropertiesWithoutUndo();
  var pipeline=Pipeline(windows);GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
  var global=GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
  if(!global){var type=typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings");global=RenderPipelineGlobalSettingsUtils.Create(type,"Assets/Settings/Aether-HDRP-GlobalSettings.asset");EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(global);}
  // Ensure the standalone quality tier cannot retain a different pipeline.
  var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);var levels=quality.FindProperty("m_QualitySettings");if(levels!=null)for(int i=0;i<levels.arraySize;i++){var field=levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");if(field!=null)field.objectReferenceValue=pipeline;}quality.ApplyModifiedPropertiesWithoutUndo();
  AssetDatabase.Refresh();
  foreach(string name in new[]{"Wayfarer","Tree","Crystal","Lantern"}){var path="Assets/Resources/Models/"+name+".fbx";var imp=AssetImporter.GetAtPath(path) as ModelImporter;if(imp!=null){imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.isReadable=true;imp.SaveAndReimport();}}
  Aether.PhotorealImport.Prepare();
  MaterialAsset("Assets/Resources/Surface.mat",Shader.Find("HDRP/Lit"));MaterialAsset("Assets/Resources/PhotorealTerrain.mat",Shader.Find("HDRP/TerrainLit"));MaterialAsset("Assets/Resources/AirMotes.mat",Shader.Find("HDRP/Unlit"));
  var motes=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/AirMotes.mat");motes.SetFloat("_SurfaceType",1);motes.SetFloat("_BlendMode",0);motes.SetFloat("_ZWrite",0);motes.SetFloat("_CullMode",0);motes.SetFloat("_TransparentCullMode",0);motes.SetFloat("_EmissiveExposureWeight",0);HDMaterial.ValidateMaterial(motes);EditorUtility.SetDirty(motes);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var game=new GameObject("Aether runtime");game.AddComponent<Aether.AetherGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Aether.unity");
  PlayerSettings.companyName="Gilbert Studio";PlayerSettings.productName="Aether";PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"studio.gilbert.aether");
  PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=false;PlayerSettings.resizableWindow=true;
  PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.Standalone,1);
  typeof(PlayerSettings).GetMethod("SetBatchingForPlatform",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)?.Invoke(null,new object[]{BuildTarget.StandaloneWindows64,0,0});
  PlayerSettings.SetUseDefaultGraphicsAPIs(windows?BuildTarget.StandaloneWindows64:BuildTarget.StandaloneOSX,false);PlayerSettings.SetGraphicsAPIs(windows?BuildTarget.StandaloneWindows64:BuildTarget.StandaloneOSX,windows?new[]{GraphicsDeviceType.Direct3D12,GraphicsDeviceType.Direct3D11}:new[]{GraphicsDeviceType.Metal});
  PlayerSettings.allowUnsafeCode=false;var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var handler=settings.FindProperty("activeInputHandler");if(handler!=null){handler.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}AssetDatabase.SaveAssets();
 }
 static void BuildPlatform(bool windows){
  Prepare(windows);
  foreach(string name in new[]{"Wayfarer","Tree","Crystal","Lantern"})if(Resources.Load<GameObject>("Models/"+name)==null)throw new Exception("Missing Blender model: "+name);
  var target=windows?BuildTarget.StandaloneWindows64:BuildTarget.StandaloneOSX;
  var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Aether.unity"},locationPathName=windows?"Builds/Windows/Aether.exe":"Builds/Aether.app",target=target,options=BuildOptions.None});
  File.WriteAllText("Documentation/"+(windows?"build-windows-result.txt":"build-result.txt"),result.summary.result+"; errors="+result.summary.totalErrors+"; size="+result.summary.totalSize+"; HDRP 17.3; "+target+"; rayTracingAsset="+((HDRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline).currentPlatformRenderPipelineSettings.supportRayTracing);
  if(result.summary.result!=BuildResult.Succeeded||result.summary.totalErrors!=0)throw new Exception("Aether build failed or reported shader/build errors");Debug.Log(windows?"AETHER_WINDOWS_BUILD_PASSED":"AETHER_BUILD_PASSED");
 }
 static void MaterialAsset(string path,Shader shader){if(!shader)throw new Exception("Missing shader: "+path);var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null)AssetDatabase.CreateAsset(new Material(shader),path);else {m.shader=shader;EditorUtility.SetDirty(m);}}
}
