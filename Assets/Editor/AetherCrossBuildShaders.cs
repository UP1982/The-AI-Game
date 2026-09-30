using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

// Aether uses native-resolution TAA, never STP. Unity includes STP compute resources
// even when unused; their DXC-only DirectX variants cannot compile in the Mac editor.
public sealed class AetherCrossBuildShaders : IPreprocessComputeShaders {
 public int callbackOrder => -1000;
 public void OnProcessComputeShader(ComputeShader shader,string kernelName,IList<ShaderCompilerData> variants){
  if(Application.platform==RuntimePlatform.WindowsEditor||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)return;
  if(shader.name=="StpSetup"||shader.name=="StpPreTaa"||shader.name=="StpTaa"){
   variants.Clear();Debug.Log("AETHER_CROSS_BUILD: excluded unused DXC-only STP kernel "+shader.name+"/"+kernelName);
  }
 }
}
