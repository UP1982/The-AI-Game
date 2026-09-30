using UnityEngine;
namespace Aether {
public partial class WorldGeometry {
 // Architectural silhouettes are assembled from a small, deterministic material palette.
 void Landmark() {
  landmarkPosition=new Vector3(0,Height(0,14),14);
  var pale=Mat(Color.Lerp(stone.color,new Color(.79f,.75f,.62f),.42f));
  var worn=Mat(Color.Lerp(stone.color,new Color(.22f,.30f,.29f),.34f));
  var moss=Mat(Color.Lerp(Palette.Hex(data.ground,new Color(.25f,.38f,.29f)),new Color(.17f,.24f,.20f),.65f));
  LandmarkPaving(pale,worn);
  if(data.landmark=="sanctuary")Sanctuary(pale,worn,moss);
  else if(data.landmark=="monolith")Monolith(pale,worn,moss);
  else Observatory(pale,worn,moss);
  LandmarkSideRuins(pale,worn,moss);
 }
 void LandmarkPaving(Material pale,Material worn) {
  // Individual low slabs follow the ground. No tall foundation lip blocks exploration.
  for(int z=-5;z<=5;z+=2)for(int x=-5;x<=5;x+=2){
   if(x*x+z*z>38)continue;
   var p=landmarkPosition+new Vector3(x,0,z);p.y=Height(p.x,p.z)+.075f;
   var tile=Shape("Weathered precinct paving",PrimitiveType.Cube,p,new Vector3(1.93f,.15f,1.93f),(x+z)%4==0?pale:worn,true);
   tile.transform.Rotate(0,Mathf.Sin(data.seed+x*31+z*7)*1.3f,0);
  }
 }
 GameObject LandmarkBlock(string name,Vector3 local,Vector3 size,Material material,bool collide=false) {
  return Shape(name,PrimitiveType.Cube,landmarkPosition+local,size,material,collide);
 }
 void LandmarkBeam(string name,Vector3 from,Vector3 to,float width,float depth,Material material) {
  var beam=Shape(name,PrimitiveType.Cube,(from+to)*.5f,new Vector3(width,depth,Vector3.Distance(from,to)),material);
  beam.transform.rotation=Quaternion.LookRotation(to-from);
 }
 void LandmarkArc(string name,Vector3 center,float radius,float start,float end,int count,float width,Material material,Quaternion orientation) {
  for(int i=0;i<count;i++){
   float a=Mathf.Lerp(start,end,i/(float)count)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1f)/count)*Mathf.Deg2Rad;
   var p=center+orientation*new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
   var q=center+orientation*new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0);
   LandmarkBeam(name,p,q,width,width,material);
  }
 }
 void Sanctuary(Material pale,Material worn,Material moss) {
  // A roofless processional hall: three vaulted bays, broken clerestory and a quiet reflecting basin.
  for(int bay=0;bay<3;bay++){
   float z=-3.9f+bay*3.9f;
   for(int side=-1;side<=1;side+=2){
    var foot=landmarkPosition+new Vector3(side*4.05f,0,z);foot.y=Height(foot.x,foot.z);
    Shape("Sanctuary stepped column base",PrimitiveType.Cube,foot+Vector3.up*.23f,new Vector3(1.25f,.46f,1.25f),worn,true);
    for(int block=0;block<5;block++){
     float rise=.85f+block*.86f;
     Shape("Aged sanctuary column drum",PrimitiveType.Cylinder,foot+Vector3.up*rise,new Vector3(.69f,.41f,.69f),block%2==0?pale:stone,true);
     Shape("Column engraved collar",PrimitiveType.Cylinder,foot+Vector3.up*(rise+.39f),new Vector3(.76f,.035f,.76f),worn);
    }
    Shape("Sanctuary carved capital",PrimitiveType.Cube,foot+Vector3.up*4.92f,new Vector3(1.25f,.36f,1.1f),pale);
    Shape("Moss at column foot",PrimitiveType.Cube,foot+new Vector3(-side*.45f,.27f,.25f),new Vector3(.39f,.055f,.71f),moss);
    if(bay==0)LandmarkBeam("Sanctuary longitudinal cornice",landmarkPosition+new Vector3(side*4.05f,5.1f,-4.4f),landmarkPosition+new Vector3(side*4.05f,5.1f,4.4f),.32f,.35f,worn);
   }
   // The final bay is visibly fractured near its apex.
   LandmarkArc("Stone vaulted arch",landmarkPosition+new Vector3(0,4.93f,z),4.05f,0,bay==2?64:180,bay==2?5:13,.43f,pale,Quaternion.identity);
   if(bay==2)LandmarkArc("Broken vault fragment",landmarkPosition+new Vector3(0,4.93f,z),4.05f,112,180,5,.43f,worn,Quaternion.identity);
   if(bay<2)LandmarkArc("Bronze vault inlay",landmarkPosition+new Vector3(0,4.91f,z-.23f),3.97f,12,168,12,.045f,metal,Quaternion.identity);
  }
  // Fragmentary roof ribs make the silhouette read as an abandoned building, not a ring of columns.
  for(int rib=0;rib<4;rib++){
   float x=-3+rib*2;float top=4.93f+Mathf.Sqrt(4.05f*4.05f-x*x);
   LandmarkBeam("Remaining roof spine",landmarkPosition+new Vector3(x,top,-4.15f),landmarkPosition+new Vector3(x,top,rib%2==0?-.1f:2.8f),.19f,.22f,worn);
  }
  var water=Mat(Color.Lerp(Palette.Hex(data.sky,Color.gray),new Color(.06f,.25f,.27f),.65f),.5f,.08f);
  LandmarkBlock("Reflecting basin stone lip",new Vector3(0,.29f,1.2f),new Vector3(3.6f,.36f,3.6f),worn,true);
  LandmarkBlock("Still reflecting pool",new Vector3(0,.483f,1.2f),new Vector3(3.19f,.022f,3.19f),water);
  for(int side=-1;side<=1;side+=2){
   LandmarkBlock("Pool bronze rim",new Vector3(side*1.72f,.50f,1.2f),new Vector3(.055f,.035f,3.45f),metal);
   LandmarkBlock("Pool bronze rim",new Vector3(0,.50f,1.2f+side*1.72f),new Vector3(3.45f,.035f,.055f),metal);
  }
  Ring(landmarkPosition+new Vector3(0,.51f,1.2f),.8f,.04f,glow,Quaternion.Euler(90,0,0),false);
  for(int i=0;i<6;i++){var piece=LandmarkBlock("Fallen sanctuary masonry",new Vector3(5.1f-i*.15f,.28f,-2.4f+i*.8f),new Vector3(.65f,.45f,.5f),i%2==0?pale:worn,true);piece.transform.Rotate(8,i*37,5);}
 }
 void Observatory(Material pale,Material worn,Material moss) {
  // Concentric shallow terraces lead to a large armillary instrument with a horizontal telescope.
  for(int tier=0;tier<3;tier++)Shape("Observatory shallow terrace",PrimitiveType.Cylinder,landmarkPosition+Vector3.up*(.11f+tier*.20f),new Vector3(12-tier*1.1f,.10f,12-tier*1.1f),tier==1?worn:pale,true);
  for(int i=0;i<5;i++){
   float z=-7.2f+i*.50f;var p=landmarkPosition+new Vector3(0,0,z);float top=Mathf.Lerp(Height(p.x,p.z)+.13f,landmarkPosition.y+.60f,i/4f);
   Shape("Observatory approach stair",PrimitiveType.Cube,new Vector3(p.x,top-.10f,p.z),new Vector3(2.8f,.20f,.58f),pale,true);
  }
  Shape("Celestial mechanism pedestal",PrimitiveType.Cylinder,landmarkPosition+Vector3.up*1.16f,new Vector3(2.5f,.57f,2.5f),worn,true);
  Shape("Pedestal bronze upper plate",PrimitiveType.Cylinder,landmarkPosition+Vector3.up*1.78f,new Vector3(2.85f,.055f,2.85f),metal);
  var center=landmarkPosition+Vector3.up*4.5f;
  Ring(center,3.25f,.19f,metal,Quaternion.Euler(20,0,12),true);
  Ring(center,2.90f,.12f,pale,Quaternion.Euler(65,40,0),true);
  Ring(center,2.57f,.075f,glow,Quaternion.Euler(10,110,25),true);
  Ring(center,3.29f,.045f,glow,Quaternion.Euler(20,0,12),true);
  LandmarkBeam("Armillary polar axle",landmarkPosition+new Vector3(-.7f,1.8f,0),landmarkPosition+new Vector3(.7f,7.0f,0),.18f,.18f,metal);
  var lens=Shape("Ancient celestial telescope",PrimitiveType.Cylinder,center,new Vector3(.76f,2.55f,.76f),worn);lens.transform.rotation=Quaternion.Euler(71,38,0);
  var axis=lens.transform.up;
  for(int side=-1;side<=1;side+=2){
   var rim=Shape("Telescope machined brass collar",PrimitiveType.Cylinder,center+axis*(side*2.45f),new Vector3(.97f,.15f,.97f),metal);rim.transform.rotation=lens.transform.rotation;
   var glass=Shape("Telescope glass",PrimitiveType.Cylinder,center+axis*(side*2.61f),new Vector3(.73f,.016f,.73f),glow);glass.transform.rotation=lens.transform.rotation;
  }
  for(int tick=0;tick<24;tick++){
   float a=tick*Mathf.PI/12;var pos=landmarkPosition+new Vector3(Mathf.Sin(a)*4.7f,.624f,Mathf.Cos(a)*4.7f);
   var mark=Shape("Observatory azimuth engraving",PrimitiveType.Cube,pos,new Vector3(.045f,.012f,tick%3==0?.55f:.23f),metal);mark.transform.rotation=Quaternion.Euler(0,tick*15,0);
  }
  // Off-axis broken instrument piers and a readable stone chart station.
  for(int side=-1;side<=1;side+=2){
   var p=landmarkPosition+new Vector3(side*4.9f,.8f,2.2f);
   Shape("Peripheral instrument pier",PrimitiveType.Cylinder,p,new Vector3(.9f,.65f,.9f),worn,true);
   Ring(p+Vector3.up*1.35f,.65f,.09f,metal,Quaternion.Euler(0,side*25,0),false);
   LandmarkBlock("Pier moss line",new Vector3(side*4.9f,.27f,2.2f),new Vector3(1.1f,.08f,.7f),moss);
  }
  var chart=LandmarkBlock("Astronomer's stone chart",new Vector3(-3.3f,1.08f,-2.6f),new Vector3(1.45f,.18f,1.05f),worn,true);chart.transform.Rotate(16,-24,0);
  for(int line=0;line<4;line++)LandmarkBlock("Chart engraved coordinates",new Vector3(-3.3f,1.21f,-2.94f+line*.20f),new Vector3(1.08f,.018f,.028f),metal);
 }
 void Monolith(Material pale,Material worn,Material moss) {
  // Three separated, uneven slabs read as one fractured monument, with luminous seams exposed inside.
  var dark=Mat(Color.Lerp(stone.color,new Color(.105f,.18f,.20f),.73f),.18f);
  var rng=new System.Random(unchecked(data.seed^0x53716));
  float[] heights={8.6f,11.8f,9.5f};
  for(int shard=0;shard<3;shard++){
   float x=(shard-1)*1.45f,h=heights[shard];
   var slab=LandmarkBlock("Fractured memory slab",new Vector3(x,h*.5f+.22f,0),new Vector3(1.2f,h,1.55f),shard==1?dark:worn,true);slab.transform.Rotate(0,(shard-1)*8,(shard-1)*-3);
   LandmarkBlock("Exposed luminous fracture",new Vector3(x+(shard==2?-.50f:.50f),h*.52f,-.80f),new Vector3(.052f,h*.83f,.045f),glow);
   for(int rune=0;rune<7;rune++){
    float width=.18f+(float)rng.NextDouble()*.51f;
    LandmarkBlock("Monolith ancient inscription",new Vector3(x,1.5f+rune*.83f,-.809f),new Vector3(width,.028f,.023f),rune%3==0?glow:metal);
    if(rune%2==0)LandmarkBlock("Monolith inscription stroke",new Vector3(x-width*.45f,1.60f+rune*.83f,-.812f),new Vector3(.025f,.22f,.025f),metal);
   }
  }
  Ring(landmarkPosition+Vector3.up*.20f,3.6f,.055f,metal,Quaternion.Euler(90,0,0),false);
  Ring(landmarkPosition+Vector3.up*.21f,4.5f,.025f,glow,Quaternion.Euler(90,0,0),false);
  var orbit=new GameObject("Slowly orbiting monolith fragments").transform;orbit.SetParent(root.transform);orbit.position=landmarkPosition+Vector3.up*5.7f;rings.Add(orbit);
  for(int shard=0;shard<9;shard++){
   float a=shard*Mathf.PI*2/9,r=3.5f+(float)rng.NextDouble()*.8f;
   var pos=orbit.position+new Vector3(Mathf.Sin(a)*r,Mathf.Sin(a*2)*1.8f,Mathf.Cos(a)*r);
   var fragment=Shape("Suspended carved fragment",PrimitiveType.Cube,pos,new Vector3(.35f+shard%3*.18f,.55f+shard%2*.5f,.46f),shard%3==0?pale:dark);
   fragment.isStatic=false;fragment.transform.Rotate(shard*17,shard*31,shard*11);fragment.transform.SetParent(orbit,true);
  }
  for(int chip=0;chip<9;chip++){
   float a=chip*2.399f;var p=landmarkPosition+new Vector3(Mathf.Sin(a)*5.2f,0,Mathf.Cos(a)*5.2f);p.y=Height(p.x,p.z)+.22f;
   var rubble=Shape("Scattered monolith chips",PrimitiveType.Cube,p,new Vector3(.65f,.38f,.45f),chip%3==0?moss:dark,true);rubble.transform.Rotate(7,chip*27,8);
  }
 }
 void LandmarkSideRuins(Material pale,Material worn,Material moss) {
  // Stable centers let the explorer's journal refer to these optional side discoveries.
  var east=new Vector3(22,Height(22,26),26);var west=new Vector3(-23,Height(-23,18),18);
  var easternMarker=new GameObject("Eastern Survey Ruin");easternMarker.transform.SetParent(root.transform);easternMarker.transform.position=east;
  var westernMarker=new GameObject("Western Wayfarer Well");westernMarker.transform.SetParent(root.transform);westernMarker.transform.position=west;
  obstacles.Add(new Vector3(east.x,4,east.z));obstacles.Add(new Vector3(west.x,3.3f,west.z));
  bool frozen=data.biome=="tundra"||data.biome=="frozen"||data.biome=="ice";bool desert=data.biome=="desert";
  var relief=frozen?Mat(new Color(.54f,.68f,.72f),.22f):desert?Mat(new Color(.55f,.39f,.24f)):moss;
  for(int side=-1;side<=1;side+=2){
   var p=east+new Vector3(side*2.3f,0,0);p.y=Height(p.x,p.z);
   Shape("Survey ruin broad footing",PrimitiveType.Cube,p+Vector3.up*.22f,new Vector3(1.22f,.44f,1.15f),worn,true);
   for(int block=0;block<3;block++)Shape("Survey ruin surviving jamb",PrimitiveType.Cube,p+Vector3.up*(.84f+block*.73f),new Vector3(.74f,.69f,.75f),block%2==0?pale:worn,true);
   Shape("Survey ruin carved capital",PrimitiveType.Cube,p+Vector3.up*2.86f,new Vector3(1.2f,.32f,1.1f),pale);
   Shape("Survey ruin weathered relief",PrimitiveType.Cube,p+new Vector3(0,.42f,-.38f),new Vector3(.54f,.27f,.065f),relief);
  }
  LandmarkArc("Eastern broken survey arch",east+Vector3.up*2.96f,2.3f,0,65,5,.4f,pale,Quaternion.identity);
  LandmarkArc("Eastern broken survey arch",east+Vector3.up*2.96f,2.3f,98,180,6,.4f,pale,Quaternion.identity);
  Ring(east+new Vector3(0,2.9f,.12f),1.25f,.055f,metal,Quaternion.identity,false);
  LandmarkBeam("Ruined survey instrument pointer",east+new Vector3(-.83f,2.07f,.13f),east+new Vector3(.83f,3.73f,.13f),.05f,.05f,metal);
  Shape("Survey ruin scattered voussoir",PrimitiveType.Cube,east+new Vector3(1.2f,.22f,-1.3f),new Vector3(.76f,.42f,.52f),worn,true).transform.Rotate(8,28,18);
  for(int i=0;i<5;i++){
   var p=east+new Vector3(-1.5f+i*.73f,0,-2.1f);p.y=Height(p.x,p.z)+.075f;
   Shape("Survey ruin stone approach",PrimitiveType.Cube,p,new Vector3(.66f,.15f,1.1f),worn,true);
  }
  // Hollow polygonal well curb, with a suspended weathered ring rather than another central orb.
  for(int block=0;block<12;block++){
   float a=block*Mathf.PI/6;var p=west+new Vector3(Mathf.Sin(a)*1.72f,0,Mathf.Cos(a)*1.72f);p.y=Height(p.x,p.z)+.39f;
   var wall=Shape("Wayfarer well curb stone",PrimitiveType.Cube,p,new Vector3(.92f,.76f,.42f),block%3==0?worn:pale,true);wall.transform.Rotate(0,block*30,0);
   Shape("Wayfarer well weather stains",PrimitiveType.Cube,p+new Vector3(Mathf.Sin(a)*.225f,.16f,Mathf.Cos(a)*.225f),new Vector3(.22f,.18f,.075f),relief).transform.Rotate(0,block*30,0);
  }
  var water=Mat(frozen?new Color(.44f,.62f,.69f):new Color(.065f,.19f,.21f),.4f,.04f);
  Shape(frozen?"Frozen well mirror":"Well dark reflecting surface",PrimitiveType.Cylinder,west+Vector3.up*.12f,new Vector3(2.91f,.015f,2.91f),water);
  for(int side=-1;side<=1;side+=2){
   var p=west+new Vector3(side*2.35f,0,0);p.y=Height(p.x,p.z);
   Shape("Well old support pier",PrimitiveType.Cube,p+Vector3.up*1.6f,new Vector3(.43f,3.2f,.5f),worn,true);
   Shape("Well support bronze collar",PrimitiveType.Cube,p+Vector3.up*2.81f,new Vector3(.52f,.14f,.59f),metal);
  }
  Shape("Well overhead lintel",PrimitiveType.Cube,west+Vector3.up*3.22f,new Vector3(5.4f,.29f,.7f),desert?worn:pale);
  Ring(west+Vector3.up*2.72f,.39f,.075f,metal,Quaternion.identity,false);
  LandmarkBeam("Well suspended line",west+new Vector3(.1f,2.65f,0),west+new Vector3(.1f,.45f,0),.034f,.034f,metal);
  Model("Lantern",west+new Vector3(-2.6f,Height(west.x-2.6f,west.z-1.8f)-west.y,-1.8f),.65f);
 }
 void Gate(float x) {
  float side=x<0?1:-1;var p=new Vector3(x,Height(x,0),0);
  var pale=Mat(Color.Lerp(stone.color,new Color(.79f,.75f,.62f),.35f));var dark=Mat(Color.Lerp(stone.color,new Color(.12f,.22f,.24f),.47f));
  for(int flank=-1;flank<=1;flank+=2){
   float z=flank*3.15f;var foot=new Vector3(x,Height(x,z),z);obstacles.Add(new Vector3(x,.88f,z));
   Shape("Horizon gate footing",PrimitiveType.Cube,foot+Vector3.up*.25f,new Vector3(1.7f,.50f,1.6f),dark,true);
   for(int course=0;course<5;course++)Shape("Horizon gate stone courses",PrimitiveType.Cube,foot+Vector3.up*(.95f+course*.89f),new Vector3(1.03f,.85f,1.02f),course%2==0?pale:stone,true);
   Shape("Horizon gate crowned capital",PrimitiveType.Cube,foot+Vector3.up*5.04f,new Vector3(1.62f,.4f,1.5f),pale);
   Shape("Gate bronze vertical inlay",PrimitiveType.Cube,foot+new Vector3(side*.53f,2.65f,0),new Vector3(.035f,3.9f,.055f),metal);
   for(int glyph=0;glyph<5;glyph++){
    Shape("Horizon rune",PrimitiveType.Cube,foot+new Vector3(side*.55f,1.20f+glyph*.72f,.23f),new Vector3(.022f,.06f,.28f),glow);
    Shape("Horizon rune stem",PrimitiveType.Cube,foot+new Vector3(side*.55f,1.30f+glyph*.72f,.34f),new Vector3(.024f,.23f,.035f),metal);
   }
   Model("Lantern",foot+new Vector3(side*1.0f,0,flank*.9f),.85f);
  }
  Shape("Horizon gate ancient lintel",PrimitiveType.Cube,p+Vector3.up*5.29f,new Vector3(1.45f,.55f,7.8f),pale);
  Shape("Horizon gate bronze cornice",PrimitiveType.Cube,p+Vector3.up*5.64f,new Vector3(1.55f,.12f,8.0f),metal);
  Shape("Gate central keystone",PrimitiveType.Cube,p+Vector3.up*5.60f,new Vector3(1.60f,1.1f,.92f),dark);
  Ring(p+new Vector3(side*.82f,5.63f,0),.30f,.055f,glow,Quaternion.Euler(0,90,0),false);
  // A partial luminous arc frames the threshold without putting collision in the route.
  LandmarkArc("Horizon half halo",p+Vector3.up*2.85f,2.46f,0,180,16,.055f,glow,Quaternion.Euler(0,90,0));
  for(int tread=-2;tread<=2;tread++){
   float tx=x+tread*.78f;Shape("Threshold paving",PrimitiveType.Cube,new Vector3(tx,Height(tx,0)+.075f,0),new Vector3(.75f,.15f,4.3f),dark,true);
  }
 }
}
}
