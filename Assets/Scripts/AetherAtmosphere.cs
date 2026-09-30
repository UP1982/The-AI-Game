using UnityEngine;
namespace Aether {
// A single reusable mesh provides subtle pollen, windborne sand or snow.
public sealed class AetherAtmosphere : MonoBehaviour {
 const int Count=140;
 WorldGeometry geometry; Mesh mesh; Material material; GameObject particles;Texture2D radial;
 readonly Vector3[] vertices=new Vector3[Count*4];readonly Color[] colors=new Color[Count*4];
 readonly Vector3[] seeds=new Vector3[Count];readonly float[] sizes=new float[Count];
 Region region;int mode;float clock;
 void Start(){
  geometry=GetComponent<WorldGeometry>();var template=Resources.Load<Material>("AirMotes");if(!template){enabled=false;return;}
  material=new Material(template);radial=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(1-d)*.4f);}radial.SetPixels(pixels);radial.Apply();
  material.SetTexture("_UnlitColorMap",radial);material.SetFloat("_SurfaceType",1);material.SetFloat("_BlendMode",0);material.SetFloat("_ZWrite",0);material.SetFloat("_CullMode",0);material.SetFloat("_TransparentCullMode",0);material.SetFloat("_EmissiveExposureWeight",0);UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);
  mesh=new Mesh{name="Windborne particles"};mesh.MarkDynamic();
  particles=new GameObject("Pollen, sand and snow",typeof(MeshFilter),typeof(MeshRenderer));particles.transform.SetParent(transform,false);
  particles.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=particles.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  var uv=new Vector2[Count*4];var triangles=new int[Count*6];
  for(int i=0;i<Count;i++){int v=i*4,t=i*6;uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(0,1);triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}
  mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
 }
 void LateUpdate(){
  if(!geometry||geometry.data==null||!Camera.main||!mesh)return;
  if(region!=geometry.data){region=geometry.data;clock=0;mode=region.biome=="grove"?0:region.biome=="desert"?1:2;var random=new System.Random(region.seed+987);
   for(int i=0;i<Count;i++){seeds[i]=new Vector3((float)random.NextDouble()*40,(float)random.NextDouble()*10,(float)random.NextDouble()*40);sizes[i]=.02f+(float)random.NextDouble()*.04f;}}
  clock+=Time.deltaTime;var camera=Camera.main.transform;var eye=camera.position;
  var right=camera.right;var up=camera.up;Color color=mode==0?new Color(.91f,.95f,.60f):mode==1?new Color(.80f,.67f,.43f):new Color(.87f,.95f,1);material.SetColor("_UnlitColor",new Color(color.r,color.g,color.b,.22f));
  for(int i=0;i<Count;i++){
   var seed=seeds[i];float speed=mode==1?1.4f:.3f;
   float x=Mathf.Repeat(seed.x+clock*speed-eye.x,40)-20+eye.x,z=Mathf.Repeat(seed.z+clock*.12f-eye.z,40)-20+eye.z;
   float baseHeight=geometry.Height(x,z);float y=mode==2?baseHeight+Mathf.Repeat(seed.y-clock*.42f,10):baseHeight+.4f+seed.y*(mode==0?.55f:.3f)+Mathf.Sin(clock*.5f+i)*.25f;
   Vector3 center=new Vector3(x,y,z);float distance=Vector3.Distance(eye,center);color.a=Mathf.Clamp01((18-distance)/7)*Mathf.Clamp01((distance-.7f)/2)*(mode==1?.26f:.55f);
   if(mode==0)color.a*=.45f+.55f*Mathf.Pow(Mathf.Sin(clock*.8f+i),2);
   float visibleSize=sizes[i]*Mathf.Sqrt(color.a);var r=right*visibleSize;var u=up*visibleSize;int v=i*4;
   vertices[v]=center-r-u;vertices[v+1]=center+r-u;vertices[v+2]=center+r+u;vertices[v+3]=center-r+u;
   for(int j=0;j<4;j++)colors[v+j]=color;
  }
  mesh.vertices=vertices;mesh.colors=colors;mesh.bounds=new Bounds(eye,new Vector3(80,80,80));
 }
 void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);if(particles)Destroy(particles);if(radial)Destroy(radial);}
}
}
