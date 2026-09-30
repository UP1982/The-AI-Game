using System;
using System.Collections.Generic;
using UnityEngine;
namespace Aether {
public partial class WorldGeometry : MonoBehaviour {
 public Region data; public Transform[] residents=new Transform[3]; public Vector3[] targets=new Vector3[3];
 public readonly List<Material> materials=new List<Material>(); public List<Transform> rings=new List<Transform>();
 readonly List<Mesh> meshes=new List<Mesh>();
 readonly List<UnityEngine.Object> terrainObjects=new List<UnityEngine.Object>();
 readonly HashSet<MeshFilter> staticCandidates=new HashSet<MeshFilter>();
 readonly Dictionary<PrimitiveType,Mesh> readablePrimitives=new Dictionary<PrimitiveType,Mesh>();
 readonly Dictionary<string,Material> materialCache=new Dictionary<string,Material>();
 readonly List<Vector3> obstacles=new List<Vector3>(); // x/z position, radius in y.
 readonly List<Vector3>[] routes={new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
 GameObject root,rainRoot; Transform[] rain=new Transform[90]; Material groundMat,stone,metal,glow; float time; public Vector3 landmarkPosition;
 public static Region Preview() { return new Region {name="The unwritten shore",description="A world waiting to be imagined.",biome="grove",sky="#A4C8C8",ground="#557A65",accent="#EAB66D",seed=4225,landmark="observatory",landmarkName="The sleeping lens",weather="clear",discovery="",npcs=new[]{new Resident{name="Wayfarer",role="Waiting for a world",goal="Watch the horizon",greeting="",color="#659A91",x=-4,z=-8},new Resident{name="",color="#A1A7C8",x=14,z=9},new Resident{name="",color="#CBAB77",x=-15,z=10}}}; }
 public float Height(float x,float z) {
  float height=SculptedHeight(x,z);
  float east=new Vector2(x-22,z-26).magnitude,west=new Vector2(x+23,z-18).magnitude;
  if(east<10)height=Mathf.Lerp(SculptedHeight(22,26),height,Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,10,east)));
  if(west<10)height=Mathf.Lerp(SculptedHeight(-23,18),height,Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,10,west)));
  return height;
 }
 float SculptedHeight(float x,float z) {
  float seed=Mathf.Abs(data.seed%997)*.137f;
  float broad=Mathf.PerlinNoise(x*.024f+seed,z*.024f+31);
  float detail=Mathf.PerlinNoise(x*.085f+19,z*.085f+seed);
  float height;
  if(data.biome=="desert") {
   float dune=Mathf.Sin(x*.16f+z*.065f+(broad-.5f)*3.2f);
   float terrace=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.72f,broad))*3.5f;
   height=dune*2.7f+(broad-.5f)*5+terrace+(detail-.5f)*.35f;
  } else if(data.biome=="tundra") {
   float ridge=1-Mathf.Abs(broad*2-1);
   height=Mathf.Pow(ridge,3)*8-2+(detail-.5f)*2.1f+Mathf.Sin(x*.18f+z*.11f)*.55f;
  } else height=(broad-.42f)*9+(detail-.5f)*1.1f+Mathf.Sin(z*.09f+x*.025f)*.65f;
  float gateCorridor=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,13,Mathf.Abs(z)));
  float mainCorridor=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,15,Mathf.Abs(x)));
  float beyondMain=Mathf.SmoothStep(0,1,Mathf.InverseLerp(28,38,Mathf.Abs(z)));
  mainCorridor=Mathf.Lerp(mainCorridor,1,beyondMain);
  float sanctum=Mathf.SmoothStep(0,1,Mathf.InverseLerp(9.5f,20,new Vector2(x,z-14).magnitude));
  return height*gateCorridor*mainCorridor*sanctum;
 }
 Material Mat(Color c,float metallic=0,float emission=0) {
  string key=ColorUtility.ToHtmlStringRGBA(c)+":"+metallic+":"+emission;
  if(materialCache.TryGetValue(key,out var cached))return cached;
  var m=PhotorealMaterials.CreateSurface(c,metallic,emission);materialCache[key]=m;materials.Add(m);return m;
 }
 GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 s,Material m,bool collide=false) {
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root.transform);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;
  var filter=g.GetComponent<MeshFilter>();
  if(filter.sharedMesh&&!filter.sharedMesh.isReadable){var replacement=ReadablePrimitive(type);if(replacement)filter.sharedMesh=replacement;}
  if(name!="Raindrop")staticCandidates.Add(filter);
  if(!collide)Destroy(g.GetComponent<Collider>());return g;
 }
 GameObject Model(string name,Vector3 p,float scale,Color? tint=null) {
  var prefab=Resources.Load<GameObject>("Models/"+name);GameObject g;
  if(prefab!=null){g=Instantiate(prefab,p,Quaternion.identity,root.transform);g.transform.localScale=Vector3.one*scale;}
  else g=Shape(name,PrimitiveType.Capsule,p,Vector3.one*scale,stone);
  foreach(var r in g.GetComponentsInChildren<Renderer>()) {
   var filter=r.GetComponent<MeshFilter>();if(filter){if(name=="Wayfarer")staticCandidates.Remove(filter);else staticCandidates.Add(filter);}
   var mats=r.sharedMaterials;string label=r.name.ToLower();
   for(int i=0;i<mats.Length;i++) {
    string key=label+" "+(mats[i]?mats[i].name.ToLower():"");Color c=stone.color;
    if(key.Contains("canopy")||key.Contains("silver jade"))c=Color.Lerp(tint??new Color(.2f,.45f,.35f),new Color(.10f,.24f,.22f),.25f);
    else if(key.Contains("trunk")||key.Contains("staff")||key.Contains("bark"))c=new Color(.22f,.28f,.26f);
    else if(key.Contains("face")||key.Contains("boot")||key.Contains("midnight"))c=new Color(.045f,.11f,.14f);
    else if(key.Contains("eye")||key.Contains("light")||key.Contains("crystal")||key.Contains("glass"))c=Color.Lerp(Palette.Hex(data.accent,new Color(.9f,.63f,.32f)),Color.white,.1f);
    else if(key.Contains("cloak")||key.Contains("teal"))c=Color.Lerp(tint??new Color(.2f,.4f,.4f),new Color(.29f,.37f,.37f),.45f);
    else if(key.Contains("collar")||key.Contains("bowl")||key.Contains("gold"))c=new Color(.6f,.39f,.19f);
    if(name=="Tree"){
     bool leaf=key.Contains("canopy")||key.Contains("jade")||key.Contains("leaves");
     Color tintColor=leaf?Color.Lerp(new Color(.25f,.39f,.14f),new Color(.40f,.47f,.22f),Mathf.Round(c.g*4)/4):Color.white;
     string cacheKey=(leaf?"leaf:":"bark:")+ColorUtility.ToHtmlStringRGB(tintColor);
     if(!materialCache.TryGetValue(cacheKey,out var treeMaterial)){treeMaterial=leaf?PhotorealMaterials.CreateLeaves(tintColor):PhotorealMaterials.CreateWood(Color.white);materialCache[cacheKey]=treeMaterial;materials.Add(treeMaterial);}mats[i]=treeMaterial;
    }else mats[i]=Mat(c,0,key.Contains("eye")||key.Contains("glass")||key.Contains("light")?.3f:0);
   }r.sharedMaterials=mats;
  }
  if(name=="Tree"||name=="Crystal"||name=="Lantern") {
   var col=g.AddComponent<CapsuleCollider>();col.radius=name=="Tree"?.26f:name=="Crystal"?.8f:.42f;
   col.height=name=="Tree"?3.4f:name=="Crystal"?2.3f:1.6f;col.center=Vector3.up*(col.height*.5f);
   obstacles.Add(new Vector3(p.x,col.radius*scale,p.z));
  }
  return g;
 }
 void ClearGenerated() {
  if(root){root.SetActive(false);Destroy(root);}
  foreach(var mesh in meshes)if(mesh)Destroy(mesh);meshes.Clear();
  foreach(var obj in terrainObjects)if(obj)Destroy(obj);terrainObjects.Clear();
  foreach(var material in materials)if(material)Destroy(material);materials.Clear();materialCache.Clear();
  rings.Clear();obstacles.Clear();staticCandidates.Clear();readablePrimitives.Clear();foreach(var route in routes)route.Clear();
 }
 public void Build(Region region) {
  ClearGenerated();
  data=region; root=new GameObject("AI Region - "+region.name);root.transform.SetParent(transform);
  var grass=Palette.Hex(data.ground,new Color(.3f,.48f,.4f)); var accent=Palette.Hex(data.accent,new Color(.95f,.65f,.3f));
  grass=Color.Lerp(grass,new Color(grass.grayscale,grass.grayscale,grass.grayscale),.27f)*.78f;grass.a=1;
  stone=Mat(new Color(.52f,.55f,.52f));metal=Mat(new Color(.68f,.45f,.23f),.65f);glow=Mat(accent,0,.7f);
  groundMat=PhotorealMaterials.CreateVertexSurface(Color.white,"Stone",.45f);materials.Add(groundMat);
  var sky=Palette.Hex(data.sky,new Color(.58f,.72f,.75f));sky=Color.Lerp(sky,new Color(.60f,.68f,.72f),.3f);
  var horizon=Color.Lerp(sky,new Color(.85f,.85f,.79f),.42f);Camera.main.backgroundColor=horizon;
  TerrainMesh(grass);
  var rng=new System.Random(data.seed);Func<float> rand=()=> (float)rng.NextDouble();
  BuildBiomeDetails();
  // Distant weathered rock formations retain the world's suspended-island setting.
  for(int i=0;i<12;i++){
   float a=i*2.399f,r=150+rand()*100;var p=new Vector3(Mathf.Sin(a)*r,-24+rand()*6,Mathf.Cos(a)*r);
   DistantIsland(p,16+rand()*14,Mat(new Color(.42f,.43f,.40f)));
  }
  BuildPaths();
  Landmark();Gate(-36);Gate(36);
  obstacles.Add(new Vector3(landmarkPosition.x,7.6f,landmarkPosition.z));
  for(int i=0;i<6;i++){float x=i%2==0?-3.2f:3.2f,z=-15+i*5;Model("Lantern",new Vector3(x,Height(x,z),z),.7f);}
  for(int i=0;i<3;i++) {
   var n=data.npcs[i];var p=new Vector3(n.x,Height(n.x,n.z),n.z);
   if(!ClearRoute(p,p)){int cell=ClosestClear(p);if(cell>=0){p=GridPoint(cell);p.y=Height(p.x,p.z);}}
   targets[i]=p;
   residents[i]=Model("Wayfarer",p,1.2f,Palette.Hex(n.color,accent)).transform;
   var col=residents[i].gameObject.AddComponent<CapsuleCollider>();col.height=1.9f;col.radius=.42f;col.center=Vector3.up*.95f;
  }

  CombineStaticGeometry();
  rainRoot=new GameObject("Rain");rainRoot.transform.SetParent(root.transform);var rainMaterial=Mat(Color.Lerp(sky,Color.white,.5f));
  for(int i=0;i<rain.Length;i++){var drop=Shape("Raindrop",PrimitiveType.Cube,Vector3.zero,new Vector3(.013f,.55f,.013f),rainMaterial);drop.transform.SetParent(rainRoot.transform);drop.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rain[i]=drop.transform;}
  SetWeather(data.weather);
 }
 void DistantIsland(Vector3 position,float radius,Material material) {
  const int count=7;var vertices=new List<Vector3>();var triangles=new List<int>();
  for(int i=0;i<count;i++) {
   float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
   var p=new Vector3(Mathf.Sin(a)*radius,Mathf.Sin(i*4.3f)*.8f,Mathf.Cos(a)*radius*.7f);
   var q=new Vector3(Mathf.Sin(b)*radius,Mathf.Sin((i+1)%count*4.3f)*.8f,Mathf.Cos(b)*radius*.7f);
   int n=vertices.Count;vertices.Add(p);vertices.Add(q);vertices.Add(Vector3.zero);vertices.Add(q);vertices.Add(p);vertices.Add(new Vector3(radius*.15f,-radius*1.7f,0));
   for(int j=0;j<6;j++)triangles.Add(n+j);
  }
  var mesh=new Mesh{name="Distant floating rock"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();meshes.Add(mesh);
  var g=new GameObject("Distant floating island",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root.transform);g.transform.position=position;
  g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;
  Shape("Distant ruin",PrimitiveType.Cube,position+Vector3.up*2.5f,new Vector3(2,5,2),material);
 }
 Color TerrainColor(Vector3 p,Color basis) {
  float patch=Mathf.PerlinNoise(p.x*.06f+data.seed%71,p.z*.06f+18);
  float fine=Mathf.PerlinNoise(p.x*.31f+41,p.z*.31f+7);
  float slope=new Vector2(Height(p.x+.6f,p.z)-Height(p.x-.6f,p.z),Height(p.x,p.z+.6f)-Height(p.x,p.z-.6f)).magnitude/1.2f;
  Color color;
  if(data.biome=="desert") {
   color=Color.Lerp(basis*.75f,Color.Lerp(basis,new Color(.75f,.62f,.43f),.5f),patch);
   color=Color.Lerp(color,new Color(.38f,.30f,.24f),Mathf.Clamp01(slope*.5f)*.4f);
   color*=.94f+Mathf.Sin(p.y*2.4f)*.025f;
  } else if(data.biome=="tundra") {
   color=Color.Lerp(new Color(.27f,.35f,.40f),Color.Lerp(basis,new Color(.76f,.83f,.82f),.65f),Mathf.Clamp01(patch+.3f-slope*.3f));
   color=Color.Lerp(color,new Color(.24f,.30f,.35f),Mathf.Clamp01(slope*.55f)*.65f);
  } else {
   color=Color.Lerp(Color.Lerp(basis,new Color(.16f,.25f,.22f),.45f),Color.Lerp(basis,new Color(.47f,.48f,.30f),.3f),patch);
   color=Color.Lerp(color,new Color(.30f,.32f,.29f),Mathf.Clamp01(slope*.4f)*.45f);
  }
  color*=.94f+fine*.12f;color.a=1;return color;
 }
 void TerrainMesh(Color baseColor) {
  const int n=82;const float step=1.5f;
  var points=new Vector3[(n+1)*(n+1)];var shades=new Color[points.Length];
  for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;float px=(x-n/2)*step,pz=(z-n/2)*step;points[i]=new Vector3(px,Height(px,pz),pz);shades[i]=TerrainColor(points[i],baseColor);}
  const int resolution=257;const float width=123, floor=-16, vertical=48;
  var terrainData=new TerrainData{name="Photographic biome heightfield",heightmapResolution=resolution,alphamapResolution=256,baseMapResolution=1024,size=new Vector3(width,vertical,width)};terrainObjects.Add(terrainData);
  var heights=new float[resolution,resolution];
  for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)heights[z,x]=(Height(x*width/(resolution-1)-width*.5f,z*width/(resolution-1)-width*.5f)-floor)/vertical;
  terrainData.SetHeights(0,0,heights);
  string baseSet=data.biome=="desert"?"Sand":data.biome=="tundra"?"Snow":"GroundForest";
  var layers=new TerrainLayer[2];
  for(int i=0;i<2;i++){
   string set=i==0?baseSet:"Stone";var diffuse=PhotorealMaterials.Texture(set,"Diffuse");var normal=PhotorealMaterials.Texture(set,"Normal");var mask=PhotorealMaterials.Texture(set,"Mask");
   if(!diffuse||!normal||!mask)throw new InvalidOperationException("Missing photographic terrain maps: "+set);
   layers[i]=new TerrainLayer{name=set,diffuseTexture=diffuse,normalMapTexture=normal,maskMapTexture=mask,tileSize=Vector2.one*(i==0?3.5f:4.5f),normalScale=.75f,smoothness=.15f,metallic=0,
    diffuseRemapMin=Vector4.zero,diffuseRemapMax=Vector4.one,maskMapRemapMin=new Vector4(0,.35f,0,.03f),maskMapRemapMax=new Vector4(0,1,1,.8f)};
   terrainObjects.Add(layers[i]);
  }
  terrainData.terrainLayers=layers;var alpha=new float[256,256,2];
  for(int z=0;z<256;z++)for(int x=0;x<256;x++){float px=x*width/255-width*.5f,pz=z*width/255-width*.5f;float slope=new Vector2(Height(px+.5f,pz)-Height(px-.5f,pz),Height(px,pz+.5f)-Height(px,pz-.5f)).magnitude;float rocky=Mathf.Clamp01((slope-.25f)*.8f)*.8f;alpha[z,x,0]=1-rocky;alpha[z,x,1]=rocky;}
  terrainData.SetAlphamaps(0,0,alpha);
  var g=Terrain.CreateTerrainGameObject(terrainData);g.name="Photographic walkable terrain";g.transform.SetParent(root.transform);g.transform.position=new Vector3(-width*.5f,floor,-width*.5f);
  var terrain=g.GetComponent<Terrain>();
  // This small, runtime-created island uses the non-instanced path so player rendering never
  // depends on a scene-discovered instancing variant. The editor scene contains no Terrain.
  terrain.drawInstanced=false;terrain.heightmapPixelError=3;terrain.basemapDistance=300;terrain.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  var template=Resources.Load<Material>("PhotorealTerrain");if(!template)throw new InvalidOperationException("PhotorealTerrain material is missing.");
  var terrainMaterial=new Material(template);materials.Add(terrainMaterial);
  UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(terrainMaterial);
  terrainMaterial.EnableKeyword("_NORMALMAP");terrainMaterial.EnableKeyword("_MASKMAP");terrainMaterial.DisableKeyword("_TERRAIN_8_LAYERS");
  terrain.materialTemplate=terrainMaterial;terrainData.SetBaseMapDirty();terrain.Flush();
  Debug.Log("AETHER_TERRAIN_READY: "+baseSet+" + Stone, diffuse/normal/mask loaded, layers="+terrainData.terrainLayers.Length+", instancing="+terrain.drawInstanced);
  // A dark faceted skirt joins the exact terrain rim rather than exposing a rectangular slab.
  var skirtVertices=new List<Vector3>();var skirtTriangles=new List<int>();
  for(int edge=0;edge<4;edge++)for(int i=0;i<n;i++) {
   int ia=edge==0?i:edge==1?i*(n+1)+n:edge==2?n*(n+1)+n-i:(n-i)*(n+1);
   int ib=edge==0?i+1:edge==1?(i+1)*(n+1)+n:edge==2?n*(n+1)+n-i-1:(n-i-1)*(n+1);
   var top=points[ia];var next=points[ib];var low=new Vector3(top.x*.82f,-18-Mathf.PerlinNoise(top.x*.1f,top.z*.1f)*8,top.z*.82f);var lowNext=new Vector3(next.x*.82f,-18-Mathf.PerlinNoise(next.x*.1f,next.z*.1f)*8,next.z*.82f);
   int k=skirtVertices.Count;skirtVertices.Add(top);skirtVertices.Add(next);skirtVertices.Add(low);skirtVertices.Add(next);skirtVertices.Add(lowNext);skirtVertices.Add(low);for(int j=0;j<6;j++)skirtTriangles.Add(k+j);
  }
  var skirt=new Mesh{name="Island rock strata"};skirt.SetVertices(skirtVertices);skirt.SetTriangles(skirtTriangles,0);skirt.RecalculateNormals();meshes.Add(skirt);
  var rock=new GameObject("Island cliffs",typeof(MeshFilter),typeof(MeshRenderer));rock.transform.SetParent(root.transform);rock.GetComponent<MeshFilter>().sharedMesh=skirt;rock.GetComponent<MeshRenderer>().sharedMaterial=Mat(Color.Lerp(baseColor,new Color(.10f,.17f,.20f),.7f));
 }
 void BuildPaths() {
  var random=new System.Random(data.seed^9237);var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
  Action<float,float,float,float> slab=(x,z,width,length)=> {
   float angle=((float)random.NextDouble()-.5f)*.15f;var center=new Vector2(x,z);var corners=new Vector2[6];
   for(int i=0;i<6;i++){float a=angle+i*Mathf.PI*2/6;corners[i]=center+new Vector2(Mathf.Cos(a)*width,Mathf.Sin(a)*length)*( .90f+(float)random.NextDouble()*.10f);}
   Color color=Color.Lerp(new Color(.37f,.40f,.36f),new Color(.60f,.60f,.50f),(float)random.NextDouble());if(data.biome=="desert")color=Color.Lerp(color,new Color(.62f,.48f,.34f),.3f);
   for(int i=0;i<6;i++) {
    var a=corners[i];var b=corners[(i+1)%6];int index=vertices.Count;
    vertices.Add(new Vector3(x,Height(x,z)+.065f,z));vertices.Add(new Vector3(b.x,Height(b.x,b.y)+.065f,b.y));vertices.Add(new Vector3(a.x,Height(a.x,a.y)+.065f,a.y));
    for(int k=0;k<3;k++){triangles.Add(index+k);colors.Add(color);}
   }
  };
  for(int i=0;i<32;i++){float z=-27+i*1.10f;if(Mathf.Abs(z)<1.3f)continue;slab(-.65f+((float)random.NextDouble()-.5f)*.18f,z,.64f,.51f);slab(.66f,z+.13f,.65f,.48f);}
  for(int i=0;i<65;i++){float x=-36+i*1.125f;slab(x,-.55f,.53f,.51f);slab(x+.08f,.57f,.51f,.49f);}
  foreach(var destination in new[]{new Vector2(22,26),new Vector2(-23,18)}) {
   var origin=new Vector2(0,14);var direction=(destination-origin).normalized;float distance=Vector2.Distance(origin,destination);
   for(float along=7.5f;along<distance-4.3f;along+=1.25f){var at=origin+direction*along;slab(at.x,at.y,.47f,.48f);}
  }
  var mesh=new Mesh{name="Weathered terrain-following path"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();meshes.Add(mesh);
  var path=new GameObject("Ancient causeways",typeof(MeshFilter),typeof(MeshRenderer));path.transform.SetParent(root.transform);path.GetComponent<MeshFilter>().sharedMesh=mesh;path.GetComponent<MeshRenderer>().sharedMaterial=groundMat;
 }
 Mesh ReadablePrimitive(PrimitiveType type) {
  if(readablePrimitives.TryGetValue(type,out var cached))return cached;
  if(type!=PrimitiveType.Cube&&type!=PrimitiveType.Cylinder&&type!=PrimitiveType.Sphere)return null;
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();
  if(type==PrimitiveType.Cube) {
   foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}) {
    var u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var v=Vector3.Cross(normal,u);int k=vertices.Count;
    vertices.Add((normal-u-v)*.5f);vertices.Add((normal+u-v)*.5f);vertices.Add((normal+u+v)*.5f);vertices.Add((normal-u+v)*.5f);
    for(int i=0;i<4;i++)normals.Add(normal);indices.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
   }
  } else if(type==PrimitiveType.Cylinder) {
   const int sides=20;
   for(int i=0;i<sides;i++) {
    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;var p=new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f);var q=new Vector3(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f);int k=vertices.Count;
    vertices.Add(p-Vector3.up);vertices.Add(p+Vector3.up);vertices.Add(q-Vector3.up);vertices.Add(q+Vector3.up);
    normals.Add(p.normalized);normals.Add(p.normalized);normals.Add(q.normalized);normals.Add(q.normalized);indices.AddRange(new[]{k,k+1,k+2,k+2,k+1,k+3});
    k=vertices.Count;vertices.Add(Vector3.up);vertices.Add(q+Vector3.up);vertices.Add(p+Vector3.up);for(int j=0;j<3;j++){normals.Add(Vector3.up);indices.Add(k+j);}
    k=vertices.Count;vertices.Add(Vector3.down);vertices.Add(p-Vector3.up);vertices.Add(q-Vector3.up);for(int j=0;j<3;j++){normals.Add(Vector3.down);indices.Add(k+j);}
   }
  } else {
   const int latitude=10,longitude=16;
   for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++){float a=x*Mathf.PI*2/longitude,b=y*Mathf.PI/latitude;var normal=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));vertices.Add(normal*.5f);normals.Add(normal);}
   for(int y=0;y<latitude;y++)for(int x=0;x<longitude;x++){int k=y*(longitude+1)+x;indices.AddRange(new[]{k,k+1,k+longitude+1,k+1,k+longitude+2,k+longitude+1});}
  }
  var mesh=new Mesh{name="Readable "+type};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();meshes.Add(mesh);readablePrimitives.Add(type,mesh);return mesh;
 }
 bool IsAnimatedGeometry(Transform item) {
  foreach(var animated in rings)if(animated&&(item==animated||item.IsChildOf(animated)))return true;
  foreach(var resident in residents)if(resident&&(item==resident||item.IsChildOf(resident)))return true;
  return rainRoot&&(item==rainRoot.transform||item.IsChildOf(rainRoot.transform));
 }
 void CombineStaticGeometry() {
  // Explicit registration works in players, where editor static flags are not a runtime batching contract.
  var groups=new Dictionary<string,List<CombineInstance>>();var groupMaterials=new Dictionary<string,Material>();
  int sourceCount=0,unreadable=0,disabled=0,animatedCount=0,submeshes=0;
  foreach(var filter in staticCandidates) {
   if(!filter)continue;var renderer=filter.GetComponent<MeshRenderer>();
   if(!renderer||!renderer.enabled||!filter.sharedMesh){disabled++;continue;}
   if(IsAnimatedGeometry(filter.transform)){animatedCount++;continue;}
   if(!filter.sharedMesh.isReadable){unreadable++;continue;}
   var shared=renderer.sharedMaterials;int tileX=Mathf.FloorToInt(filter.transform.position.x/24),tileZ=Mathf.FloorToInt(filter.transform.position.z/24);bool included=false;
   for(int i=0;i<shared.Length&&i<filter.sharedMesh.subMeshCount;i++) {
    if(!shared[i])continue;string key=tileX+":"+tileZ+":"+shared[i].GetInstanceID();
    if(!groups.TryGetValue(key,out var group)){group=new List<CombineInstance>();groups.Add(key,group);groupMaterials.Add(key,shared[i]);}
    group.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});included=true;submeshes++;
   }
   if(included){renderer.enabled=false;sourceCount++;}
  }
  foreach(var pair in groups){var mesh=new Mesh{name="Region detail batch",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);meshes.Add(mesh);var batch=new GameObject("Landscape details",typeof(MeshFilter),typeof(MeshRenderer));batch.transform.SetParent(root.transform,false);batch.GetComponent<MeshFilter>().sharedMesh=mesh;batch.GetComponent<MeshRenderer>().sharedMaterial=groupMaterials[pair.Key];}
  int enabled=0;foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.enabled&&renderer.gameObject.activeInHierarchy)enabled++;
  Debug.Log("AETHER_BATCH region="+data.name+" candidates="+staticCandidates.Count+" sources="+sourceCount+" submeshes="+submeshes+" batches="+groups.Count+" unreadableSkipped="+unreadable+" disabledSkipped="+disabled+" animatedSkipped="+animatedCount+" readablePrimitiveFallbacks="+readablePrimitives.Count+" enabledBeforeRain="+enabled);
 }
 void Ring(Vector3 pos,float radius,float tube,Material mat,Quaternion rotation,bool animate) {
  // One torus renderer replaces 64 cylinder renderers for each animated ring.
  const int sides=64,section=6;var vertices=new Vector3[sides*section];var normals=new Vector3[vertices.Length];var triangles=new int[sides*section*6];
  for(int i=0;i<sides;i++)for(int j=0;j<section;j++) {
   float a=i*Mathf.PI*2/sides,b=j*Mathf.PI*2/section;var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
   int v=i*section+j;normals[v]=radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);vertices[v]=radial*radius+normals[v]*tube*.5f;
   int next=((i+1)%sides)*section+j,corner=i*section+(j+1)%section,opposite=((i+1)%sides)*section+(j+1)%section,k=v*6;
   triangles[k]=v;triangles[k+1]=next;triangles[k+2]=corner;triangles[k+3]=corner;triangles[k+4]=next;triangles[k+5]=opposite;
  }
  var mesh=new Mesh{name="Celestial ring"};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();meshes.Add(mesh);
  var g=new GameObject("Celestial ring",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root.transform);g.transform.position=pos;g.transform.rotation=rotation;
  g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
  if(animate)rings.Add(g.transform);
 }
 // Residents use a small ground grid so AI destinations never move them through ruins or vegetation.
 bool ClearRoute(Vector3 from,Vector3 to) {
  var a=new Vector2(from.x,from.z);var b=new Vector2(to.x,to.z);var delta=b-a;
  foreach(var obstacle in obstacles) {
   var center=new Vector2(obstacle.x,obstacle.z);float t=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(center-a,delta)/delta.sqrMagnitude):0;
   if((center-(a+delta*t)).sqrMagnitude<(obstacle.y+.65f)*(obstacle.y+.65f))return false;
  }
  return true;
 }
 Vector3 GridPoint(int index){return new Vector3((index%49-24)*2,0,(index/49-24)*2);}
 int ClosestClear(Vector3 point,bool requireVisible=false) {
  int best=-1;float distance=float.PositiveInfinity;
  for(int i=0;i<49*49;i++){var p=GridPoint(i);float d=(new Vector2(p.x-point.x,p.z-point.z)).sqrMagnitude;if(d<distance&&ClearRoute(p,p)&&(!requireVisible||ClearRoute(point,p))){distance=d;best=i;}}
  return best;
 }
 void PlanRoute(int resident,Vector3 target) {
  var route=routes[resident];route.Clear();var start=residents[resident].position;
  target.x=Mathf.Clamp(target.x,-46,46);target.z=Mathf.Clamp(target.z,-46,46);
  if(ClearRoute(start,target)){route.Add(target);return;}
  const int count=49*49;int first=ClosestClear(start,true),last=ClosestClear(target);if(first<0||last<0)return;
  var cost=new float[count];var parent=new int[count];var closed=new bool[count];var open=new List<int>{first};
  for(int i=0;i<count;i++){cost[i]=float.PositiveInfinity;parent[i]=-1;}cost[first]=0;
  while(open.Count>0) {
   int best=0;float score=float.PositiveInfinity;
   for(int i=0;i<open.Count;i++){int node=open[i];float f=cost[node]+Vector3.Distance(GridPoint(node),GridPoint(last));if(f<score){score=f;best=i;}}
   int current=open[best];open.RemoveAt(best);if(current==last)break;closed[current]=true;
   for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++) {
    if(dx==0&&dz==0)continue;int x=current%49+dx,z=current/49+dz;if(x<0||x>=49||z<0||z>=49)continue;
    int next=z*49+x;if(closed[next]||!ClearRoute(GridPoint(current),GridPoint(next)))continue;
    float distance=cost[current]+(dx!=0&&dz!=0?2.828427f:2);if(distance>=cost[next])continue;
    cost[next]=distance;parent[next]=current;if(!open.Contains(next))open.Add(next);
   }
  }
  if(first!=last&&parent[last]<0)return;
  for(int node=last;node>=0;node=parent[node]){route.Insert(0,GridPoint(node));if(node==first)break;}
  // Never snap a resident out of an obstruction or cut across one on the first step.
  if(route.Count>0&&!ClearRoute(start,route[0])){route.Clear();return;}
  if(ClearRoute(GridPoint(last),target))route.Add(target);
 }
 public void SetWeather(string weather) {data.weather=weather;if(rainRoot)rainRoot.SetActive(weather=="rain");if(Camera.main){var rendering=Camera.main.GetComponent<AetherRendering>();if(rendering)rendering.ApplyRegion(data);}}
 void Update() {
  time+=Time.deltaTime;
  if(rainRoot&&rainRoot.activeSelf&&Camera.main){var eye=Camera.main.transform.position;for(int i=0;i<rain.Length;i++){float x=Mathf.Sin(i*73.2f)*12,z=Mathf.Cos(i*91.7f)*12,y=10-Mathf.Repeat(time*8+i*.73f,14);rain[i].position=eye+new Vector3(x,y,z);}}
  foreach(var r in rings)if(r)r.Rotate(0,Time.deltaTime*5,Time.deltaTime*2,Space.Self);
  for(int i=0;i<residents.Length;i++)if(residents[i]) {
   var t=residents[i];var route=routes[i];if(route.Count==0)continue;
   while(route.Count>1&&ClearRoute(t.position,route[1]))route.RemoveAt(0);
   var delta=route[0]-t.position;delta.y=0;
   if(delta.magnitude<.12f){route.RemoveAt(0);continue;}
   if(delta.magnitude>.01f){var step=delta.normalized*Mathf.Min(delta.magnitude,Time.deltaTime*.8f);t.position+=step;var p=t.position;p.y=Height(p.x,p.z);t.position=p;t.rotation=Quaternion.Slerp(t.rotation,Quaternion.LookRotation(delta),Time.deltaTime*3);}
  }
 }
 public void Apply(TickResponse tick){SetWeather(tick.weather);foreach(var a in tick.actions)if(a.npc>=0&&a.npc<3){targets[a.npc]=new Vector3(a.x,Height(a.x,a.z),a.z);data.npcs[a.npc].goal=a.goal;data.npcs[a.npc].x=a.x;data.npcs[a.npc].z=a.z;PlanRoute(a.npc,targets[a.npc]);}}
 void OnDestroy(){ClearGenerated();}
}
}
