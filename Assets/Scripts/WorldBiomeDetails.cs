using System;
using System.Collections.Generic;
using UnityEngine;
namespace Aether {
public partial class WorldGeometry {
 // Details share a handful of vertex-colored meshes; individual plants are not GameObjects.
 sealed class BiomeBatch {
  public readonly List<Vector3> vertices=new List<Vector3>();
  public readonly List<int> triangles=new List<int>();
  public readonly List<Color> colors=new List<Color>();
  public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc) {
   int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
   colors.Add(ca);colors.Add(cb);colors.Add(cc);
   triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);
  }
  public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color,bool twoSided=false) {
   int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
   colors.Add(color);colors.Add(color);colors.Add(color);
   triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);
   if(twoSided)Triangle(c,b,a,color);
  }
 }
 float DetailRandom(System.Random random,float low,float high){return low+(float)random.NextDouble()*(high-low);}
 bool DetailClear(float x,float z,float radius) {
  // Include the full object footprint in every exclusion, not just its center.
  if(Mathf.Abs(x)>53-radius||Mathf.Abs(z)>53-radius)return false;
  if(Mathf.Abs(x)<6+radius&&z>-28-radius&&z<28+radius)return false;
  if(Mathf.Abs(z)<5+radius)return false;
  if(new Vector2(x,z-14).magnitude<10+radius)return false;
  if(new Vector2(x,z+16).magnitude<5+radius)return false;
  if(new Vector2(x-22,z-26).magnitude<6+radius||new Vector2(x+23,z-18).magnitude<6+radius)return false;
  foreach(var end in new[]{new Vector2(22,26),new Vector2(-23,18)}) {
   var start=new Vector2(0,14);var segment=end-start;var point=new Vector2(x,z);
   float t=Mathf.Clamp01(Vector2.Dot(point-start,segment)/segment.sqrMagnitude);
   if((point-(start+segment*t)).magnitude<1.8f+radius)return false;
  }
  if(data.npcs!=null)foreach(var resident in data.npcs)
   if(new Vector2(x-resident.x,z-resident.z).magnitude<4+radius)return false;
  return true;
 }
 Vector3 DetailGround(float x,float z,float lift=0){return new Vector3(x,Height(x,z)+lift,z);}
 void DetailMesh(string name,BiomeBatch batch,Material material,bool shadows=true,bool smoothRockNormals=false) {
  if(batch.vertices.Count==0)return;
  var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
  mesh.SetVertices(batch.vertices);mesh.SetTriangles(batch.triangles,0);mesh.SetColors(batch.colors);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
  if(smoothRockNormals) {
   // Triangle batches duplicate vertices. Weld only their normals, using exact
   // shared ring positions and area weighting; other props retain authored edges.
   var sums=new Dictionary<Vector3,Vector3>();
   for(int i=0;i<batch.triangles.Count;i+=3) {
    int a=batch.triangles[i],b=batch.triangles[i+1],c=batch.triangles[i+2];
    var normal=Vector3.Cross(batch.vertices[b]-batch.vertices[a],batch.vertices[c]-batch.vertices[a]);
    for(int corner=0;corner<3;corner++) {
     var position=batch.vertices[batch.triangles[i+corner]];Vector3 sum;sums.TryGetValue(position,out sum);sums[position]=sum+normal;
    }
   }
   var normals=new Vector3[batch.vertices.Count];
   for(int i=0;i<normals.Length;i++)normals[i]=sums[batch.vertices[i]].normalized;
   mesh.normals=normals;
  }
  var child=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
  child.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
  if(!shadows)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 }
 void DetailObstacle(string name,Vector3 position,float radius,float height) {
  var g=new GameObject(name);g.transform.SetParent(root.transform,false);g.transform.position=position;
  var collider=g.AddComponent<CapsuleCollider>();collider.radius=radius;collider.height=Mathf.Max(height,radius*2);collider.center=Vector3.up*(height*.5f);
  obstacles.Add(new Vector3(position.x,radius,position.z));
 }
 void DetailBakeModel(GameObject model,BiomeBatch batch) {
  // Keep original trunk/crystal colliders and reduce the whole forest to one renderer.
  foreach(var filter in model.GetComponentsInChildren<MeshFilter>()) {
   var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
   var mesh=filter.sharedMesh;var vertices=mesh.vertices;var materials=renderer.sharedMaterials;
   for(int sub=0;sub<mesh.subMeshCount;sub++) {
    var color=sub<materials.Length&&materials[sub]?materials[sub].color:Color.white;
    var indices=mesh.GetTriangles(sub);
    for(int i=0;i<indices.Length;i+=3)batch.Triangle(filter.transform.TransformPoint(vertices[indices[i]]),filter.transform.TransformPoint(vertices[indices[i+1]]),filter.transform.TransformPoint(vertices[indices[i+2]]),color);
   }
   renderer.enabled=false;renderer.gameObject.isStatic=false;
  }
 }
 void DetailRock(BiomeBatch batch,Vector3 p,Vector3 scale,Color color,System.Random random,bool layered=false,bool collide=true) {
  p-=Vector3.up*.04f;
  bool pebble=Mathf.Max(scale.x,scale.z)<.5f;
  int sides=pebble?10:16,levels=pebble?5:layered?10:8;
  var ring=new Vector3[levels,sides];var shades=new Color[levels,sides];
  float yaw=DetailRandom(random,0,Mathf.PI*2),phase=DetailRandom(random,0,Mathf.PI*2);
  for(int level=0;level<levels;level++)for(int i=0;i<sides;i++) {
   float t=level/(float)(levels-1),a=yaw+i*Mathf.PI*2/sides;
   // Broad coherent erosion, rather than independent noise at each triangle.
   float erosion=1+.06f*Mathf.Sin(a*3+phase)+.035f*Mathf.Cos(a*5-t*2+phase);
   float silhouette=layered?Mathf.Lerp(.92f,.52f,t)+.06f*Mathf.Sin(t*21+phase):Mathf.Pow(Mathf.Sin((.20f+t*.78f)*Mathf.PI),.68f);
   float radial=silhouette*erosion;
   float drift=Mathf.Sin(t*Mathf.PI)*.09f;
   float y=t-.045f+.024f*Mathf.Sin(a*2+phase)*Mathf.Sin(t*Mathf.PI);
   ring[level,i]=p+new Vector3((Mathf.Cos(a)*radial+drift)*scale.x,y*scale.y,(Mathf.Sin(a)*radial-drift*.5f)*scale.z);
   if(level==0){var foot=ring[level,i];foot.y=Mathf.Min(foot.y,Height(foot.x,foot.z)-.035f);ring[level,i]=foot;}
   float strata=layered?.93f+.055f*Mathf.Sin(t*28+phase):.98f+.025f*Mathf.Sin(a*3+t*4+phase);
   shades[level,i]=Color.Lerp(color,Color.white,.18f)*strata;shades[level,i].a=1;
  }
  for(int level=0;level<levels-1;level++)for(int i=0;i<sides;i++) {
   int j=(i+1)%sides;
   batch.Triangle(ring[level,i],ring[level+1,i],ring[level,j],shades[level,i],shades[level+1,i],shades[level,j]);
   batch.Triangle(ring[level,j],ring[level+1,i],ring[level+1,j],shades[level,j],shades[level+1,i],shades[level+1,j]);
  }
  Vector3 top=p+Vector3.up*scale.y;var topShade=Color.Lerp(color,Color.white,.19f);
  for(int i=0;i<sides;i++){int j=(i+1)%sides;batch.Triangle(ring[levels-1,i],top,ring[levels-1,j],shades[levels-1,i],topShade,shades[levels-1,j]);}
  if(collide)DetailObstacle(layered?"Sandstone outcrop collision":"Weathered stone collision",p,Mathf.Max(scale.x,scale.z)*1.10f,scale.y);
 }
 void DetailGrass(BiomeBatch batch,Vector3 p,float size,Color color,System.Random random,bool dry=false) {
  int blades=dry?5:7;
  for(int i=0;i<blades;i++) {
   float angle=DetailRandom(random,0,Mathf.PI*2),height=size*DetailRandom(random,.55f,1.3f),width=size*(dry?.07f:.11f);
   Vector3 side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),bend=new Vector3(-side.z,0,side.x)*height*.36f;
   var basePoint=p+side*DetailRandom(random,-size*.25f,size*.25f);var tip=basePoint+Vector3.up*height+bend;
   batch.Triangle(basePoint-side*width,tip,basePoint+side*width,Color.Lerp(color,Color.white,DetailRandom(random,0,.15f)),true);
  }
 }
 void DetailFern(BiomeBatch batch,Vector3 p,float size,Color color,System.Random random) {
  float rotation=DetailRandom(random,0,Mathf.PI*2);
  for(int arm=0;arm<5;arm++) {
   float a=rotation+arm*Mathf.PI*2/5;var forward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=new Vector3(-forward.z,0,forward.x);
   for(int leaf=1;leaf<=5;leaf++) {
    float t=leaf/6f;var center=p+forward*t*size+Vector3.up*(Mathf.Sin(t*Mathf.PI)*size*.42f+.04f);
    float width=(1-t)*size*.3f;
    foreach(float sign in new[]{-1f,1f})batch.Triangle(center-forward*size*.13f,center+side*sign*width+forward*size*.13f,center+forward*size*.16f,Color.Lerp(color,Color.white,t*.22f),true);
   }
  }
 }
 void DetailFlower(BiomeBatch batch,Vector3 p,float size,Color color) {
  var tip=p+Vector3.up*size;batch.Triangle(p+Vector3.left*.017f,tip,p+Vector3.right*.017f,new Color(.22f,.36f,.23f),true);
  for(int i=0;i<5;i++) {
   float a=i*Mathf.PI*2/5,b=(i+1)*Mathf.PI*2/5;float radius=size*.29f;
   batch.Triangle(tip,tip+new Vector3(Mathf.Cos(a)*radius,.05f,Mathf.Sin(a)*radius),tip+new Vector3(Mathf.Cos(b)*radius,.05f,Mathf.Sin(b)*radius),color,true);
  }
 }
 void DetailPatch(BiomeBatch batch,Vector3 p,float radius,Color color,System.Random random) {
  const int sides=11;
  for(int i=0;i<sides;i++) {
   float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
   Vector3 first=DetailGround(p.x+Mathf.Cos(a)*radius,p.z+Mathf.Sin(a)*radius,.07f),second=DetailGround(p.x+Mathf.Cos(b)*radius,p.z+Mathf.Sin(b)*radius,.07f);
   batch.Triangle(DetailGround(p.x,p.z,.075f),second,first,Color.Lerp(color,Color.white,DetailRandom(random,0,.06f)));
  }
 }
 void DetailLog(BiomeBatch batch,Vector3 p,float length,System.Random random) {
  float angle=DetailRandom(random,0,Mathf.PI*2);var forward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var side=new Vector3(-forward.z,0,forward.x);
  var start=p-forward*length*.5f+Vector3.up*.27f;var end=p+forward*length*.5f+Vector3.up*.27f;const int sides=7;
  for(int i=0;i<sides;i++) {
   float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;var first=side*Mathf.Cos(a)*.28f+Vector3.up*Mathf.Sin(a)*.28f;var second=side*Mathf.Cos(b)*.28f+Vector3.up*Mathf.Sin(b)*.28f;
   Color bark=Color.Lerp(new Color(.16f,.21f,.16f),new Color(.33f,.34f,.23f),i/(float)sides);
   batch.Triangle(start+first,end+first,start+second,bark);batch.Triangle(start+second,end+first,end+second,bark);
   batch.Triangle(start,start+second,start+first,new Color(.57f,.43f,.27f));batch.Triangle(end,end+first,end+second,new Color(.57f,.43f,.27f));
  }
  var g=new GameObject("Fallen timber collision");g.transform.SetParent(root.transform,false);g.transform.position=p+Vector3.up*.27f;g.transform.rotation=Quaternion.LookRotation(forward);
  var collider=g.AddComponent<BoxCollider>();collider.size=new Vector3(.58f,.58f,length);
  obstacles.Add(new Vector3(p.x,length*.5f+.3f,p.z));
 }
 void DetailPot(BiomeBatch batch,Vector3 p,float size,System.Random random) {
  const int sides=9;float[] radius={.28f,.48f,.40f,.23f,.27f};float[] height={0,.25f,.65f,.86f,.94f};var rings=new Vector3[5,sides];
  for(int level=0;level<5;level++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;rings[level,i]=p+new Vector3(Mathf.Cos(a)*radius[level]*size,height[level]*size,Mathf.Sin(a)*radius[level]*size);}
  for(int level=0;level<4;level++)for(int i=0;i<sides;i++) {
   int j=(i+1)%sides;var color=Color.Lerp(new Color(.49f,.24f,.13f),new Color(.78f,.48f,.26f),DetailRandom(random,.2f,.8f));
   batch.Triangle(rings[level,i],rings[level+1,i],rings[level,j],color);batch.Triangle(rings[level,j],rings[level+1,i],rings[level+1,j],color);
  }
  var inner=p+Vector3.up*(size*.84f);for(int i=0;i<sides;i++)batch.Triangle(rings[4,i],inner,rings[4,(i+1)%sides],new Color(.17f,.12f,.09f));
  if(size>.8f)DetailObstacle("Abandoned pottery collision",p,size*.48f,size*.94f);
 }
 void DetailIce(BiomeBatch batch,Vector3 p,float radius,float height,System.Random random) {
  const int sides=5;float lean=DetailRandom(random,-.4f,.4f);var tip=p+new Vector3(lean,height,lean*.3f);
  for(int i=0;i<sides;i++) {
   float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;var first=p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);var second=p+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
   var shoulder=Vector3.Lerp(p,tip,.7f);var upperA=shoulder+(first-p)*.82f;var upperB=shoulder+(second-p)*.82f;
   var color=Color.Lerp(new Color(.24f,.54f,.64f),new Color(.79f,.92f,.96f),i/(float)sides);
   batch.Triangle(first,upperA,second,color);batch.Triangle(second,upperA,upperB,color);batch.Triangle(upperA,tip,upperB,Color.Lerp(color,Color.white,.2f));
  }
 }
 void BuildGroveForest(BiomeBatch foliage,BiomeBatch canopy) {
  // Separate placement RNG makes tree density independent of decorative mesh sampling.
  var random=new System.Random(data.seed^0x5A71D);var positions=new List<Vector2>();
  const int target=66;const float spacing=3.3f;
  for(int zone=0;zone<3;zone++) {
   int quota=zone==0?20:zone==1?45:target;
   for(int attempt=0;attempt<2400&&positions.Count<quota;attempt++) {
    float x,z;
    if(zone==0){x=(attempt%2==0?-1:1)*DetailRandom(random,12,25);z=DetailRandom(random,-25,-10);}
    else if(zone==1){x=DetailRandom(random,-46,46);z=DetailRandom(random,32,48);}
    else {x=DetailRandom(random,-48,48);z=DetailRandom(random,-43,48);}
    if(!DetailClear(x,z,2.2f))continue;var candidate=new Vector2(x,z);bool blocked=false;
    foreach(var existing in positions)if((existing-candidate).sqrMagnitude<spacing*spacing){blocked=true;break;}
    if(blocked)continue;
    foreach(var obstacle in obstacles)if(new Vector2(x-obstacle.x,z-obstacle.z).magnitude<obstacle.y+.65f){blocked=true;break;}
    if(!blocked)positions.Add(candidate);
   }
  }
  // Deterministic grid fallback guarantees dense planting even when unlucky random samples cluster.
  for(float z=-45;z<=48&&positions.Count<target;z+=3.7f)for(float x=-48;x<=48&&positions.Count<target;x+=3.7f) {
   if(!DetailClear(x,z,2.2f))continue;var candidate=new Vector2(x,z);bool blocked=false;
   foreach(var existing in positions)if((existing-candidate).sqrMagnitude<spacing*spacing){blocked=true;break;}
   foreach(var obstacle in obstacles)if(new Vector2(x-obstacle.x,z-obstacle.z).magnitude<obstacle.y+.65f){blocked=true;break;}
   if(!blocked)positions.Add(candidate);
  }
  foreach(var location in positions) {
   var p=DetailGround(location.x,location.y,-.04f);bool backdrop=location.y>31;
   float scale=DetailRandom(random,backdrop?1.2f:1.03f,backdrop?1.72f:1.48f);
   var jade=Color.Lerp(new Color(.12f,.48f,.34f),new Color(.38f,.61f,.32f),DetailRandom(random,0,1));
   var tree=Model("Tree",p,scale,jade);tree.name="Jade broadleaf grove";tree.transform.Rotate(0,DetailRandom(random,0,360),0);
   // Each tree owns a dense skirt of taller grass, broad ferns and occasional blossoms.
   for(int plant=0;plant<40;plant++) {
    float angle=DetailRandom(random,0,Mathf.PI*2),distance=DetailRandom(random,.65f,3.2f);
    float x=location.x+Mathf.Cos(angle)*distance,z=location.y+Mathf.Sin(angle)*distance;
    if(!DetailClear(x,z,.75f))continue;var ground=DetailGround(x,z,.035f);
    var color=Color.Lerp(new Color(.13f,.36f,.23f),jade,.3f);
    if(plant%5==0)DetailFern(foliage,ground,DetailRandom(random,.7f,1.15f),color,random);
    else if(plant%11==0)DetailFlower(foliage,ground,DetailRandom(random,.38f,.66f),new Color(.91f,.75f,.43f));
    else DetailGrass(foliage,ground,DetailRandom(random,.35f,.76f),color,random);
   }
  }
 }
 void BuildBiomeDetails() {
  var random=new System.Random(data.seed^0x27191);var solids=new BiomeBatch();var rocks=new BiomeBatch();var foliage=new BiomeBatch();var canopy=new BiomeBatch();var lights=new BiomeBatch();
  var ground=Palette.Hex(data.ground,new Color(.32f,.48f,.37f));var leaf=Color.Lerp(ground,new Color(.12f,.34f,.24f),.6f);
  bool desert=data.biome=="desert",tundra=data.biome=="tundra";
  // Planned clusters frame the entry, gate approaches and ruins. Seed adds controlled variation.
  var centers=new List<Vector2>();
  foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-31f,-15f,14f,34f})centers.Add(new Vector2(side*DetailRandom(random,16,34),z+DetailRandom(random,-4,4)));
  foreach(var center in centers) {
   for(int major=0;major<5;major++) {
    float angle=DetailRandom(random,0,Mathf.PI*2),distance=major==0?0:DetailRandom(random,3,9);
    float x=center.x+Mathf.Cos(angle)*distance,z=center.y+Mathf.Sin(angle)*distance;
    float radius=desert?DetailRandom(random,1.2f,3.5f):tundra?DetailRandom(random,1,2.7f):DetailRandom(random,.65f,1.2f);
    if(!desert&&!tundra&&major<4)continue; // Grove trees use density-guaranteed planting below.
    if(!DetailClear(x,z,radius+1))continue;var p=DetailGround(x,z,-.08f);
    {
     float height=desert?DetailRandom(random,2.5f,6.8f):tundra?DetailRandom(random,1.8f,4.5f):DetailRandom(random,.65f,1.6f);
     var color=desert?new Color(.59f,.40f,.25f):tundra?new Color(.31f,.39f,.46f):new Color(.30f,.38f,.31f);
     DetailRock(rocks,p,new Vector3(radius,height,radius*DetailRandom(random,.55f,.9f)),color,random,desert);
     if(tundra) {
      for(int ice=0;ice<2;ice++) {float ix=x+DetailRandom(random,-radius*.22f,radius*.22f),iz=z+DetailRandom(random,-radius*.22f,radius*.22f);if(DetailClear(ix,iz,.8f))DetailIce(solids,DetailGround(ix,iz),.35f,DetailRandom(random,1.2f,3.7f),random);}
     }
    }
   }
   for(int small=0;small<80;small++) {
    float angle=DetailRandom(random,0,Mathf.PI*2),distance=DetailRandom(random,1.4f,12);
    float x=center.x+Mathf.Cos(angle)*distance,z=center.y+Mathf.Sin(angle)*distance;
    if(!DetailClear(x,z,.6f))continue;var p=DetailGround(x,z,.035f);
    if(tundra) {
     if(small%7==0&&DetailClear(x,z,2.1f))DetailPatch(foliage,p,DetailRandom(random,.7f,2.1f),new Color(.79f,.86f,.88f),random);
     else if(small%9==0)DetailIce(lights,p,.16f,DetailRandom(random,.35f,.85f),random);
     else if(small%4==0)DetailRock(rocks,p,new Vector3(.24f,.2f,.3f),new Color(.44f,.55f,.61f),random,false,false);
     else DetailGrass(foliage,p,DetailRandom(random,.16f,.33f),new Color(.49f,.63f,.66f),random,true);
    } else if(desert) {
     if(small%13==0)DetailRock(rocks,p,new Vector3(.45f,.23f,.28f),new Color(.63f,.46f,.3f),random,false,false);
     else if(small%19==0)DetailPot(solids,p,DetailRandom(random,.5f,.95f),random);
     else DetailGrass(foliage,p,DetailRandom(random,.25f,.65f),Color.Lerp(new Color(.4f,.39f,.21f),new Color(.68f,.56f,.32f),DetailRandom(random,0,1)),random,true);
    } else {
     if(small%8==0)DetailFern(foliage,p,DetailRandom(random,.55f,1.05f),leaf,random);
     else if(small%5==0)DetailFlower(foliage,p,DetailRandom(random,.25f,.55f),small%2==0?new Color(.88f,.7f,.43f):new Color(.64f,.69f,.86f));
     else DetailGrass(foliage,p,DetailRandom(random,.23f,.63f),leaf,random);
    }
   }
   float propX=center.x+3,propZ=center.y-4;
   if(DetailClear(propX,propZ,2.5f)) {
    var p=DetailGround(propX,propZ);
    if(!desert&&!tundra)DetailLog(solids,p,DetailRandom(random,2.6f,4),random);
    else if(desert){DetailPot(solids,p,1.15f,random);DetailPot(solids,p+new Vector3(1,0,.4f),.65f,random);}
    else {var crystal=Model("Crystal",p,.65f);DetailBakeModel(crystal,lights);}
   }
  }
  // A low, continuous planting edge gives close-up scale alongside the otherwise clear main path.
  for(int i=0;i<160;i++) {
   float x=(i%2==0?-1:1)*DetailRandom(random,7,11),z=DetailRandom(random,-27,32);
   if(!DetailClear(x,z,.5f))continue;var p=DetailGround(x,z,.035f);
   if(tundra)DetailGrass(foliage,p,.3f,new Color(.52f,.65f,.7f),random,true);
   else if(desert)DetailGrass(foliage,p,.42f,new Color(.64f,.53f,.31f),random,true);
   else if(i%4==0)DetailFlower(foliage,p,.36f,new Color(.88f,.77f,.53f));else DetailGrass(foliage,p,.43f,leaf,random);
  }
  if(!desert&&!tundra)BuildGroveForest(foliage,canopy);
  var material=PhotorealMaterials.CreateVertexSurface(Color.white);materials.Add(material);
  var rockMaterial=PhotorealMaterials.CreateVertexSurface(Color.white,"Stone",.42f);rockMaterial.SetFloat("_Smoothness",.16f);materials.Add(rockMaterial);
  DetailMesh("Weathered stratified geology",rocks,rockMaterial,true,true);
  DetailMesh("Biome timber and archaeological details",solids,material);
  DetailMesh("Biome understory and ground cover",foliage,material,false);
  DetailMesh("Combined forest canopy",canopy,material);
  DetailMesh("Luminous glacial mineral clusters",lights,Mat(new Color(.56f,.82f,.9f),.1f,.35f));
 }
}
}
