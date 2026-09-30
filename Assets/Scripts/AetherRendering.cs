using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Aether {
// HDRP 17.3 runtime environment. No service calls, asset mutation, or automatic paid generation.
// APIs checked against the bundled Unity 6000.3.24f1 HDRP package sources.
public sealed class AetherRendering : MonoBehaviour {
 public string QualityLabel { get; private set; } = "High";
 public string RenderMode { get; private set; } = "HDRP raster";
 public bool RayTracingAvailable { get; private set; }
 public bool RayTracingEnabled { get; private set; }
 public string HardwareFlags { get; private set; }
 public string SkyLabel { get; private set; }
 Camera view;
 Light sun;
 HDAdditionalCameraData cameraData;
 HDAdditionalLightData lightData;
 HDRenderPipelineAsset pipelineAsset;
 GameObject environmentRoot;
 Volume environmentVolume,rayVolume;
 VolumeProfile environmentProfile,rayProfile;
 HDAdditionalReflectionData reflection;
 VisualEnvironment visual;
 HDRISky hdri;
 PhysicallyBasedSky physicalSky;
 Exposure exposure;
 Fog fog;
 ScreenSpaceReflection rasterReflections;
 ScreenSpaceAmbientOcclusion rasterOcclusion;
 ContactShadows contacts;
 HDShadowSettings shadows;
 Region region;
 string lastWeather;
 bool high,rayRequested,ready,hdriCalibrated;
 Coroutine capture;

 public static AetherRendering Setup(Camera camera,Light directionalSun) {
  if(!camera||!directionalSun)throw new ArgumentNullException("Aether HDRP requires an explorer camera and directional light.");
  var component=camera.GetComponent<AetherRendering>();
  if(!component)component=camera.gameObject.AddComponent<AetherRendering>();
  if(!component.ready)component.Initialize(camera,directionalSun);
  return component;
 }
 static string Argument(string name,string fallback) {
  var args=Environment.GetCommandLineArgs();
  for(int i=0;i<args.Length;i++) {
   if(args[i].StartsWith(name+"=",StringComparison.Ordinal))return args[i].Substring(name.Length+1).ToLowerInvariant();
   if(args[i]==name&&i+1<args.Length)return args[i+1].ToLowerInvariant();
  }
  return fallback;
 }
 void Initialize(Camera camera,Light directionalSun) {
  view=camera;sun=directionalSun;
  pipelineAsset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
  high=Argument("--graphics","high")!="medium";rayRequested=Argument("--raytracing","on")!="off";
  cameraData=camera.GetComponent<HDAdditionalCameraData>();if(!cameraData)cameraData=camera.gameObject.AddComponent<HDAdditionalCameraData>();
  cameraData.clearColorMode=HDAdditionalCameraData.ClearColorMode.Sky;
  cameraData.volumeLayerMask=1;cameraData.volumeAnchorOverride=camera.transform;
  cameraData.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
  cameraData.customRenderingSettings=true;
  view.allowHDR=true;view.allowMSAA=false;QualitySettings.antiAliasing=0;
  lightData=sun.GetComponent<HDAdditionalLightData>();if(!lightData)lightData=sun.gameObject.AddComponent<HDAdditionalLightData>();
  sun.type=LightType.Directional;sun.lightUnit=LightUnit.Lux;sun.shadows=LightShadows.Soft;sun.intensity=35000;RenderSettings.sun=sun;
  lightData.angularDiameter=.65f;lightData.interactsWithSky=true;lightData.affectDiffuse=true;lightData.affectSpecular=true;lightData.lightDimmer=1;
  lightData.useContactShadow.useOverride=true;lightData.useContactShadow.@override=true;
  environmentRoot=new GameObject("Aether physical environment");
  environmentVolume=environmentRoot.AddComponent<Volume>();environmentVolume.isGlobal=true;environmentVolume.priority=20;environmentVolume.weight=1;
  environmentProfile=ScriptableObject.CreateInstance<VolumeProfile>();environmentProfile.name="Aether runtime raster environment";environmentVolume.sharedProfile=environmentProfile;
  visual=environmentProfile.Add<VisualEnvironment>(true);visual.skyAmbientMode.value=SkyAmbientMode.Dynamic;visual.renderingSpace.value=RenderingSpace.Camera;
  bool requestHdri=Argument("--hdri","on")!="off";
  var calibration=Resources.Load<TextAsset>("Photoreal/sky-lux");float measuredLux=0;
  hdriCalibrated=calibration&&float.TryParse(calibration.text,NumberStyles.Float,CultureInfo.InvariantCulture,out measuredLux)&&measuredLux>0&&!float.IsInfinity(measuredLux);
  var cube=requestHdri&&hdriCalibrated?Resources.Load<Cubemap>("Photoreal/Sky"):null;
  if(requestHdri&&!cube)Debug.Log("AETHER_HDRI physical sky fallback: calibrated Photoreal/Sky cubemap or Photoreal/sky-lux metadata unavailable.");
  if(cube) {
   hdri=environmentProfile.Add<HDRISky>(true);hdri.hdriSky.value=cube;hdri.updateMode.value=EnvironmentUpdateMode.OnChanged;
   hdri.upperHemisphereLuxValue.value=measuredLux;hdri.skyIntensityMode.value=SkyIntensityMode.Lux;hdri.desiredLuxValue.value=18000;
   visual.skyType.value=(int)SkyType.HDRI;SkyLabel="Calibrated HDRI environment";
  } else {
   physicalSky=environmentProfile.Add<PhysicallyBasedSky>(true);physicalSky.updateMode.value=EnvironmentUpdateMode.OnChanged;physicalSky.type.value=PhysicallyBasedSkyModel.EarthSimple;physicalSky.skyIntensityMode.value=SkyIntensityMode.Exposure;physicalSky.exposure.value=1.0f;
   physicalSky.airTint.value=Color.white;physicalSky.aerosolTint.value=new Color(.92f,.91f,.88f);physicalSky.groundTint.value=new Color(.13f,.15f,.12f);
   visual.skyType.value=(int)SkyType.PhysicallyBased;SkyLabel="Physical atmosphere";
  }
  exposure=environmentProfile.Add<Exposure>(true);exposure.mode.value=ExposureMode.Fixed;exposure.fixedExposure.value=11.8f;
  var tone=environmentProfile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
  var grade=environmentProfile.Add<ColorAdjustments>(true);grade.contrast.value=5;grade.saturation.value=-5;
  var bloom=environmentProfile.Add<Bloom>(true);bloom.intensity.value=.12f;bloom.threshold.value=1.2f;bloom.scatter.value=.6f;
  fog=environmentProfile.Add<Fog>(true);fog.enabled.value=true;fog.colorMode.value=FogColorMode.SkyColor;fog.baseHeight.value=-4;fog.maximumHeight.value=45;fog.maxFogDistance.value=700;fog.depthExtent.value=100;fog.anisotropy.value=.2f;
  fog.screenResolutionPercentage.value=12.5f;fog.volumeSliceCount.value=48;
  contacts=environmentProfile.Add<ContactShadows>(true);contacts.enable.value=true;contacts.length.value=.12f;contacts.opacity.value=.7f;contacts.maxDistance.value=40;
  shadows=environmentProfile.Add<HDShadowSettings>(true);shadows.cascadeShadowSplitCount.value=4;shadows.cascadeShadowSplit0.value=.08f;shadows.cascadeShadowSplit1.value=.25f;shadows.cascadeShadowSplit2.value=.55f;
  rasterReflections=environmentProfile.Add<ScreenSpaceReflection>(true);rasterReflections.enabled.value=true;rasterReflections.enabledTransparent.value=false;rasterReflections.tracing.value=RayCastingMode.RayMarching;rasterReflections.reflectSky.value=true;rasterReflections.usedAlgorithm.value=ScreenSpaceReflectionAlgorithm.PBRAccumulation;rasterReflections.minSmoothness=.55f;rasterReflections.smoothnessFadeStart=.75f;rasterReflections.quality.Override(ScalableSettingLevelParameter.LevelCount);
  rasterOcclusion=environmentProfile.Add<ScreenSpaceAmbientOcclusion>(true);rasterOcclusion.rayTracing.value=false;rasterOcclusion.intensity.value=.7f;rasterOcclusion.radius.value=.85f;rasterOcclusion.directLightingStrength.value=.15f;rasterOcclusion.temporalAccumulation.value=true;rasterOcclusion.quality.Override(ScalableSettingLevelParameter.LevelCount);
  CreateReflectionProbe();CreateRayProfile();
  ready=true;SetQuality(high?"high":"medium");
  Debug.Log("AETHER_HDRP "+HardwareFlags+" quality="+QualityLabel+" mode="+RenderMode+" sky="+SkyLabel);
 }
 void CameraFeature(FrameSettingsField field,bool enabled) {
  cameraData.renderingPathCustomFrameSettings.SetEnabled(field,enabled);
  cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;
 }
 void CreateReflectionProbe() {
  var captureObject=new GameObject("Regional reflection capture");captureObject.transform.SetParent(environmentRoot.transform);captureObject.transform.position=new Vector3(0,3.5f,8);
  var native=captureObject.AddComponent<ReflectionProbe>();native.mode=ReflectionProbeMode.Realtime;native.refreshMode=ReflectionProbeRefreshMode.ViaScripting;native.size=new Vector3(140,65,140);native.hdr=true;
  reflection=captureObject.GetComponent<HDAdditionalReflectionData>();if(!reflection)reflection=captureObject.AddComponent<HDAdditionalReflectionData>();
  reflection.mode=ProbeSettings.Mode.Realtime;reflection.realtimeMode=ProbeSettings.RealtimeMode.OnDemand;reflection.timeSlicing=true;
  reflection.influenceVolume.shape=InfluenceShape.Box;reflection.influenceVolume.boxSize=new Vector3(140,65,140);reflection.influenceVolume.boxBlendDistancePositive=Vector3.one*8;reflection.influenceVolume.boxBlendDistanceNegative=Vector3.one*8;
  reflection.settingsRaw.cubeResolution.useOverride=true;reflection.settingsRaw.cubeResolution.@override=CubeReflectionResolution.CubeReflectionResolution256;reflection.PrepareCulling();
 }
 void CreateRayProfile() {
  bool platform=Application.platform==RuntimePlatform.WindowsPlayer;
  bool dx12=SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12;
  bool assetEnabled=pipelineAsset&&pipelineAsset.currentPlatformRenderPipelineSettings.supportRayTracing;
  RayTracingAvailable=platform&&dx12&&SystemInfo.supportsRayTracing&&assetEnabled;
  HardwareFlags="gpu="+SystemInfo.graphicsDeviceName+" api="+SystemInfo.graphicsDeviceType+" platform="+Application.platform+" deviceRayTracing="+SystemInfo.supportsRayTracing+" assetRayTracing="+assetEnabled+" eligible="+RayTracingAvailable;
  var rayObject=new GameObject("Optional Windows ray tracing");rayObject.transform.SetParent(environmentRoot.transform);
  rayVolume=rayObject.AddComponent<Volume>();rayVolume.isGlobal=true;rayVolume.priority=30;rayVolume.weight=0;
  rayProfile=ScriptableObject.CreateInstance<VolumeProfile>();rayProfile.name="Aether optional Windows ray reflections and AO";rayVolume.sharedProfile=rayProfile;
  var reflections=rayProfile.Add<ScreenSpaceReflection>(true);reflections.enabled.value=true;reflections.enabledTransparent.value=false;reflections.tracing.value=RayCastingMode.RayTracing;reflections.reflectSky.value=true;
  reflections.mode.value=pipelineAsset&&(pipelineAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode&RenderPipelineSettings.SupportedRayTracingMode.Performance)!=0?RayTracingMode.Performance:RayTracingMode.Quality;
  reflections.quality.Override(ScalableSettingLevelParameter.LevelCount);reflections.minSmoothness=.5f;reflections.smoothnessFadeStart=.72f;reflections.rayLength=70;reflections.fullResolution=false;reflections.sampleCount.value=1;reflections.bounceCount.value=1;reflections.denoise=true;
  var occlusion=rayProfile.Add<ScreenSpaceAmbientOcclusion>(true);occlusion.quality.Override(ScalableSettingLevelParameter.LevelCount);occlusion.rayTracing.value=true;occlusion.intensity.value=.65f;occlusion.rayLength=1.5f;occlusion.sampleCount=1;occlusion.denoise=true;
 }
 public void SetQuality(string quality) {
  if(!ready)return;high=!string.Equals(quality,"medium",StringComparison.OrdinalIgnoreCase);QualityLabel=high?"High":"Medium";
  bool ssr=pipelineAsset&&pipelineAsset.currentPlatformRenderPipelineSettings.supportSSR,ao=pipelineAsset&&pipelineAsset.currentPlatformRenderPipelineSettings.supportSSAO;
  bool volumetrics=high&&pipelineAsset&&pipelineAsset.currentPlatformRenderPipelineSettings.supportVolumetrics;
  RayTracingEnabled=RayTracingAvailable&&rayRequested&&high&&ssr&&ao;rayVolume.weight=RayTracingEnabled?1:0;rayVolume.enabled=RayTracingEnabled;
  RenderMode=RayTracingEnabled?"HDRP ray-traced reflections + AO":"HDRP raster / screen-space lighting";
  cameraData.TAAQuality=high?HDAdditionalCameraData.TAAQualityLevel.High:HDAdditionalCameraData.TAAQualityLevel.Medium;
  foreach(var field in new[]{FrameSettingsField.Postprocess,FrameSettingsField.Antialiasing,FrameSettingsField.Bloom,FrameSettingsField.ColorGrading,FrameSettingsField.Tonemapping,FrameSettingsField.AtmosphericScattering,FrameSettingsField.ExposureControl,FrameSettingsField.ReflectionProbe,FrameSettingsField.SkyReflection,FrameSettingsField.DirectSpecularLighting,FrameSettingsField.ContactShadows})CameraFeature(field,true);
  CameraFeature(FrameSettingsField.SSR,ssr);CameraFeature(FrameSettingsField.SSAO,ao);CameraFeature(FrameSettingsField.Volumetrics,volumetrics);CameraFeature(FrameSettingsField.ReprojectionForVolumetrics,volumetrics);CameraFeature(FrameSettingsField.RayTracing,RayTracingEnabled);
  // Deliberately avoid motion blur and chromatic aberration for comfortable exploration.
  CameraFeature(FrameSettingsField.MotionBlur,false);CameraFeature(FrameSettingsField.DepthOfField,false);CameraFeature(FrameSettingsField.ChromaticAberration,false);
  rasterReflections.enabled.value=ssr;rasterReflections.rayMaxIterations=high?48:24;
  rasterOcclusion.intensity.value=ao?.7f:0;rasterOcclusion.fullResolution=false;rasterOcclusion.stepCount=high?6:4;rasterOcclusion.directionCount=high?2:1;rasterOcclusion.maximumRadiusInPixels=high?48:32;
  fog.enableVolumetricFog.value=volumetrics;shadows.maxShadowDistance.value=high?110:75;shadows.cascadeShadowSplitCount.value=high?4:3;
  lightData.SetShadowResolution(high?2048:1024);lightData.SetShadowResolutionOverride(true);
  reflection.settingsRaw.cubeResolution.@override=high?CubeReflectionResolution.CubeReflectionResolution256:CubeReflectionResolution.CubeReflectionResolution128;
  if(region!=null)UpdateRegionLighting();RefreshEnvironment();
 }
 public void ApplyRegion(Region next) {
  if(!ready||next==null)return;region=next;lastWeather=next.weather;UpdateRegionLighting();RefreshEnvironment();
 }
 void UpdateRegionLighting() {
  bool desert=region.biome=="desert",tundra=region.biome=="tundra",wet=region.weather=="rain",mist=region.weather=="mist";
  sun.transform.rotation=Quaternion.Euler(desert?31:tundra?23:36,-35+(region.seed%23),0);
  sun.color=desert?new Color(1,.89f,.73f):tundra?new Color(.88f,.94f,1):new Color(1,.96f,.87f);
  sun.intensity=wet?10000:mist?18000:desert?48000:tundra?29000:35000;
  exposure.fixedExposure.value=wet?10.8f:mist?11.2f:desert?12.0f:11.8f;
  fog.meanFreePath.value=wet?100:mist?72:desert?320:230;fog.maximumHeight.value=mist?32:55;fog.albedo.value=wet?new Color(.69f,.76f,.79f):tundra?new Color(.83f,.90f,.94f):new Color(.90f,.90f,.85f);
  if(hdri){hdri.desiredLuxValue.value=wet?7500:mist?11000:18000;hdri.rotation.value=Mathf.Repeat(region.seed,360);}
  Debug.Log("AETHER_LIGHTING biome="+region.biome+" weather="+region.weather+" sunLux="+sun.intensity+" cameraEV="+exposure.fixedExposure.value+" skyID="+visual.skyType.value+" skyInteracts="+lightData.interactsWithSky);
  if(physicalSky){physicalSky.aerosolDensity.value=desert?.022f:mist?.035f:wet?.030f:.012f;physicalSky.groundTint.value=desert?new Color(.29f,.22f,.15f):tundra?new Color(.5f,.56f,.6f):new Color(.10f,.16f,.11f);}
 }
 public void RefreshEnvironment() {
  if(!ready)return;if(capture!=null)StopCoroutine(capture);capture=StartCoroutine(CaptureAfterBuild());
 }
 IEnumerator CaptureAfterBuild() {
  // Wait until destroyed previous-region renderers are gone and the active volume stack has updated.
  yield return null;yield return null;
  var pipeline=RenderPipelineManager.currentPipeline as HDRenderPipeline;if(pipeline!=null)pipeline.RequestSkyEnvironmentUpdate();
  yield return null;if(reflection)reflection.RequestRenderNextUpdate();
  Debug.Log("AETHER_LIGHT_CAPTURE configuredEV="+exposure.fixedExposure.value+" configuredSky="+visual.skyType.value+" sunLux="+sun.intensity+" reflectionMode="+(reflection?reflection.mode.ToString():"none"));capture=null;
 }
 void LateUpdate() {
  if(ready&&region!=null&&lastWeather!=region.weather){lastWeather=region.weather;UpdateRegionLighting();RefreshEnvironment();}
 }
 void OnDestroy() {
  if(environmentRoot)Destroy(environmentRoot);ReleaseProfile(environmentProfile);ReleaseProfile(rayProfile);
 }
 static void ReleaseProfile(VolumeProfile profile) {
  if(!profile)return;foreach(var component in profile.components)if(component)Destroy(component);Destroy(profile);
 }
}
}
