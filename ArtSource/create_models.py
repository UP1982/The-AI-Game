"""Create Aether's original game props and origin-centered Unity exports.

Run with Blender --background --python ArtSource/create_models.py.
The FBX files retain gameplay-facing mesh/material names; only the editable
source scene is arranged side by side after the exports are complete.
"""
import bpy, math, os, json, random
from mathutils import Vector
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT=os.path.join(ROOT,'Assets','Resources','Models')
os.makedirs(OUT,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1.0
def mat(name,c):
 m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); return m
stone=mat('Chalk ceramic',(0.79,0.83,0.78)); dark=mat('Midnight enamel',(.045,.12,.16)); brass=mat('Old gold',(.72,.43,.18)); cloth=mat('Wayfarer teal',(.12,.40,.42)); wood=mat('Weathered bark',(.18,.24,.26)); leaf=mat('Silver jade',(.29,.57,.47)); glow=mat('Amber glass',(1,.58,.22))
def cone(name,loc,r1,r2,depth,m,verts=8):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=depth,location=loc); o=bpy.context.object; o.name=name; o.data.materials.append(m); return o
def sphere(name,loc,scale,m):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(m); return o
def cube(name,loc,scale,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;o.data.materials.append(m);return o
def beam(name,start,end,r1,r2,m,verts=7):
 direction=Vector(end)-Vector(start)
 o=cone(name,(Vector(start)+Vector(end))*.5,r1,r2,direction.length,m,verts)
 o.rotation_euler=direction.to_track_quat('Z','Y').to_euler()
 return o
def ring_mesh(name,rings,m,sides=7,phase=0):
 """Horizontal irregular rings keep the ground contact precisely at zero."""
 vertices=[]
 for x,y,z,r in rings:
  for i in range(sides):
   a=phase+i*math.tau/sides;variation=1+.10*math.sin(i*8.7)
   vertices.append((x+math.cos(a)*r*variation,y+math.sin(a)*r*variation,z))
 faces=[tuple(reversed(range(sides)))]
 for j in range(len(rings)-1):
  for i in range(sides):
   k=(i+1)%sides;faces.append((j*sides+i,j*sides+k,(j+1)*sides+k,(j+1)*sides+i))
 faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
 uv=mesh.uv_layers.new(name='UVMap')
 for face in mesh.polygons:
  for loop in face.loop_indices:
   vertex=mesh.loops[loop].vertex_index;ring=vertex//sides;around=vertex%sides
   # Duplicate the UV seam in loop space without splitting geometry.
   u=around/sides
   if around==0 and any(v%sides==sides-1 for v in face.vertices):u=1
   uv.data[loop].uv=(u,rings[ring][2])
 o=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(o);o.data.materials.append(m)
 return o
def bevel_edges(obj,width=.015):
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bevel=obj.modifiers.new('Fine worn edge','BEVEL');bevel.width=width;bevel.segments=2
 bpy.ops.object.modifier_apply(modifier=bevel.name)
 return obj
groups=[]
def export(name,start):
 objects=[o for o in bpy.context.scene.objects if o.name not in start]
 collection=bpy.data.collections.new(name)
 bpy.context.scene.collection.children.link(collection)
 for o in objects:
  for previous in list(o.users_collection):previous.objects.unlink(o)
  collection.objects.link(o)
 bpy.context.view_layer.update()
 corners=[o.matrix_world @ Vector(corner) for o in objects for corner in o.bound_box]
 lower=[min(point[axis] for point in corners) for axis in range(3)]
 upper=[max(point[axis] for point in corners) for axis in range(3)]
 assert lower[2]>=-0.001, name+' extends below its ground origin'
 print('AETHER_MODEL_BOUNDS '+json.dumps({'name':name,'meshes':len(objects),'triangles':sum(sum(len(face.vertices)-2 for face in o.data.polygons) for o in objects),'min':[round(v,3) for v in lower],'max':[round(v,3) for v in upper],'dimensions':[round(upper[i]-lower[i],3) for i in range(3)]}))
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
 groups.append(objects)
 for o in objects:o.hide_set(True)
 return objects
# Human-scale traveler: original modeled cloth and accessories, no downloaded mesh.
start=set(o.name for o in bpy.context.scene.objects)
def traveler_smooth(obj):
 for face in obj.data.polygons:face.use_smooth=True
 return obj
def traveler_round(name,position,scale,material,segments=20,rings=12):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=position)
 obj=bpy.context.object;obj.name=name;obj.scale=scale;obj.data.materials.append(material)
 return traveler_smooth(obj)
def traveler_mesh(name,vertices,faces,material):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);mesh.materials.append(material)
 return traveler_smooth(obj)
def traveler_cloth(name,rings,material,fold=.02,sides=56):
 vertices=[];faces=[]
 for level,(z,rx,ry) in enumerate(rings):
  for i in range(sides):
   a=i*math.tau/sides
   # Long folds share their phase vertically, with subtle drape to either side.
   pleat=fold*(.65*math.sin(a*13+level*.14)+.35*math.sin(a*21-level*.1))
   hem=.018*(math.sin(a*3)+.45*math.cos(a*7)) if level==0 else 0
   vertices.append((math.cos(a)*(rx+pleat),math.sin(a)*(ry+pleat*.8),z+hem))
 for level in range(len(rings)-1):
  for i in range(sides):
   j=(i+1)%sides;a=level*sides+i;b=level*sides+j
   faces.append((a,b,b+sides,a+sides))
 obj=traveler_mesh(name,vertices,faces,material)
 uv=obj.data.uv_layers.new(name='UVMap')
 for face in obj.data.polygons:
  for loop in face.loop_indices:
   index=obj.data.loops[loop].vertex_index;around=index%sides
   u=around/sides
   if around==0 and any(v%sides==sides-1 for v in face.vertices):u=1
   uv.data[loop].uv=(u,index//sides/(len(rings)-1))
 return obj
# Slightly asymmetric, ankle-length outer cloth reads as fabric rather than a cone.
traveler_cloth('Flowing cloak folded wool',[(.20,.31,.22),(.32,.305,.21),(.48,.29,.205),(.65,.27,.195),(.81,.245,.18),(.94,.23,.165),(1.08,.245,.17),(1.22,.275,.18),(1.33,.29,.17),(1.39,.19,.13)],cloth,.023)
# The separate shoulder mantle settles over the sleeves and has its own scalloped hem.
traveler_cloth('Cloak shoulder mantle',[(1.11,.32,.208),(1.19,.36,.21),(1.29,.37,.195),(1.37,.25,.16),(1.42,.14,.115)],cloth,.011,48)
# Tailored front closure and a small antique clasp.
traveler_smooth(beam('Cloak front seam',(0,-.232,.25),(0,-.181,1.17),.009,.007,dark,10))
traveler_round('Gold mantle clasp',(0,-.211,1.24),(.035,.014,.025),brass,16,10)
# Hood is an actual open cloth shell; the face sits inside its front opening.
hood_vertices=[];hood_faces=[];hood_sides=48;hood_rows=17
for row in range(hood_rows):
 t=.055+row/(hood_rows-1)*(math.pi*.75-.055)
 for i in range(hood_sides):
  a=i*math.tau/hood_sides;ruffle=1+.015*math.sin(a*7+t*2)
  hood_vertices.append((math.cos(a)*math.sin(t)*.205*ruffle,.02+math.cos(t)*.205,1.565+math.sin(a)*math.sin(t)*.255*ruffle))
for row in range(hood_rows-1):
 for i in range(hood_sides):
  j=(i+1)%hood_sides;a=row*hood_sides+i;b=row*hood_sides+j
  hood_faces.append((a,b,b+hood_sides,a+hood_sides))
hood=traveler_mesh('Cloak hood open shell',hood_vertices,hood_faces,cloth)
# A rolled edge gives the opening a physical thickness without sealing it.
for i in range(hood_sides):
 a=hood_vertices[(hood_rows-1)*hood_sides+i];b=hood_vertices[(hood_rows-1)*hood_sides+(i+1)%hood_sides]
 traveler_smooth(beam('Cloak hood rolled hem',a,b,.007,.007,cloth,6))
traveler_round('Face recessed mask',(0,-.066,1.57),(.112,.045,.143),dark,24,16)
# Restrained eye slits and a sculpted central mask ridge retain the quiet identity.
for x in (-.040,.040):traveler_round('Eye bronze slit',(x,-.110,1.606),(.018,.004,.0045),brass,12,8)
traveler_smooth(beam('Face mask seam',(0,-.111,1.53),(0,-.111,1.63),.004,.003,dark,8))
# Relaxed sleeves and gloved hands; right hand rests around the walking stick.
for side in (-1,1):
 shoulder=(side*.27,.018,1.27);elbow=(side*.34,-.015,1.04)
 wrist=(side*(.405 if side==1 else .36),-.08 if side==1 else -.018,.865)
 traveler_smooth(beam('Cloak upper sleeve',shoulder,elbow,.105,.087,cloth,20))
 traveler_round('Cloak elbow fold',elbow,(.087,.082,.09),cloth,16,10)
 traveler_smooth(beam('Cloak lower sleeve',elbow,wrist,.087,.059,cloth,20))
 traveler_round('Boot leather glove',wrist,(.062,.057,.087),dark,16,10)
 traveler_round('Boot glove thumb',(wrist[0]-side*.04,wrist[1]-.045,wrist[2]+.014),(.022,.030,.041),dark,12,8)
# Layered boots expose their soles and toe shape below the cloth hem.
for x in (-.135,.135):
 sole=cube('Boot stitched sole',(x,-.048,.024),(.155,.31,.048),dark)
 bpy.context.view_layer.objects.active=sole;sole.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bevel_edges(sole,.012)
 traveler_round('Boot shaped toe',(x,-.073,.095),(.078,.165,.075),dark,20,12)
 traveler_smooth(cone('Boot ankle leather',(x,.015,.22),.066,.070,.22,dark,20))
 traveler_round('Boot ankle cuff',(x,.015,.305),(.078,.083,.031),dark,16,10)
# Soft leather waist belt and a satchel worn on the left hip.
bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=8,major_radius=.242,minor_radius=.022,location=(0,0,.94))
belt=bpy.context.object;belt.name='Boot leather waist belt';belt.scale.y=.74;belt.data.materials.append(dark);traveler_smooth(belt)
bevel_edges(cube('Gold belt buckle',(0,-.194,.94),(.059,.024,.053),brass),.003)
traveler_round('Boot leather satchel',(-.285,.075,.84),(.115,.074,.17),dark,24,16)
traveler_round('Boot leather satchel flap',(-.29,-.001,.90),(.119,.024,.092),dark,20,12)
traveler_round('Gold satchel stud',(-.29,-.024,.87),(.013,.008,.013),brass,12,8)
# A ribbon strap follows the chest instead of passing straight through the body.
strap_path=[(.23,-.142,1.34),(.11,-.185,1.22),(-.02,-.190,1.09),(-.15,-.178,.97),(-.28,-.09,.86)]
strap_vertices=[]
for point in strap_path:
 for offset in (-.024,.024):strap_vertices.append((point[0]+offset,point[1]-.012,point[2]+offset))
strap_faces=[]
for i in range(len(strap_path)-1):
 a=i*2;strap_faces.extend([(a,a+2,a+3,a+1),(a+1,a+3,a+2,a)])
traveler_mesh('Boot leather diagonal strap',strap_vertices,strap_faces,dark)
traveler_smooth(beam('Walking staff',(.405,-.08,0),(.405,-.08,1.74),.023,.019,wood,16))
traveler_round('Gold staff cap',(.405,-.08,1.765),(.032,.032,.045),brass,16,10)
# Keep the traveler economical in the renderer: four material groups, while all
# individual surfaces remain selectable as linked geometry in Blender Edit Mode.
for material,name in ((cloth,'Cloak tailored traveler'),(dark,'Face boot leather details'),(brass,'Gold traveler hardware'),(wood,'Walking staff')):
 parts=[obj for obj in bpy.context.scene.objects if obj.name not in start and obj.type=='MESH' and len(obj.data.materials)==1 and obj.data.materials[0]==material]
 if not parts:continue
 bpy.ops.object.select_all(action='DESELECT')
 for obj in parts:obj.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bpy.context.object.name=name
traveler_triangles=sum(sum(len(face.vertices)-2 for face in obj.data.polygons) for obj in bpy.context.scene.objects if obj.name not in start)
assert traveler_triangles<=15000, 'Wayfarer exceeds its 15k triangle budget'
export('Wayfarer',start)
# Tree geometry stays in two meshes to keep instanced forest draw calls modest.
# Bark uses cylindrical UVs, and each real leaf has its own full 0..1 UV island.
start=set(o.name for o in bpy.context.scene.objects)
rng=random.Random(83942)
wood_vertices=[];wood_faces=[];wood_uv=[]
leaf_vertices=[];leaf_faces=[];leaf_uv=[]
def tree_tube(points,radii,sides=7):
 points=[Vector(p) for p in points];base=len(wood_vertices);travel=0
 for j,(point,radius) in enumerate(zip(points,radii)):
  tangent=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized()
  reference=Vector((0,0,1)) if abs(tangent.z)<.93 else Vector((1,0,0))
  right=tangent.cross(reference).normalized();across=tangent.cross(right).normalized()
  if j:travel+=(point-points[j-1]).length
  for k in range(sides+1):
   a=k*math.tau/sides;irregular=1+.055*math.sin(k*3.8+j*.6)
   v=point+(right*math.cos(a)+across*math.sin(a))*radius*irregular
   # Root contact rings rest exactly on the terrain, never below it.
   v.z=max(0,v.z)
   wood_vertices.append(tuple(v));wood_uv.append((k/sides,travel/.8))
 for j in range(len(points)-1):
  for k in range(sides):
   a=base+j*(sides+1)+k;b=a+sides+1
   wood_faces.append((a,a+1,b+1,b))
 # End caps are included only at thin branch tips; the bases intersect parent wood.
 wood_faces.append(tuple(base+(len(points)-1)*(sides+1)+k for k in range(sides)))
def tree_leaf(position,direction,length,width,twist):
 direction=Vector(direction).normalized()
 across=direction.cross(Vector((0,0,1)))
 if across.length<.05:across=direction.cross(Vector((1,0,0)))
 across.normalize();normal=across.cross(direction).normalized()
 across2=across*math.cos(twist)+normal*math.sin(twist)
 normal=across2.cross(direction).normalized();position=Vector(position)
 # Six outline corners plus a raised midrib make a curved, pointed leaf.
 outline=[(0,0),(.42,.24),(.5,.57),(0,1),(-.5,.57),(-.42,.24)]
 local=[(x,y,.025*math.sin(math.pi*y)) for x,y in outline]+[(0,.48,.11)]
 # Independent back vertices preserve normals when both sides are visible.
 for back in (False,True):
  base=len(leaf_vertices)
  for x,y,z in local:
   v=position+direction*(y*length)+across2*(x*width)+normal*(z*length)
   leaf_vertices.append(tuple(v));leaf_uv.append((x+.5,y))
  for k in range(6):
   face=(base+6,base+k,base+(k+1)%6)
   leaf_faces.append(tuple(reversed(face)) if back else face)
def finish_tree_mesh(name,vertices,faces,uvs,material):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
 uv=mesh.uv_layers.new(name='UVMap')
 for face in mesh.polygons:
  face.use_smooth=True
  for loop in face.loop_indices:uv.data[loop].uv=uvs[mesh.loops[loop].vertex_index]
 obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);mesh.materials.append(material)
 return obj
trunk=[(0,0,0),(.04,.02,.25),(-.08,.03,1.1),(.09,-.03,2.0),(.22,.08,2.9),(.15,.12,3.8),(.32,.06,4.7),(.22,.12,5.6),(.30,.15,6.1)]
tree_tube(trunk,[.34,.27,.22,.18,.14,.105,.075,.04,.006],12)
for i in range(6):
 a=i*math.tau/6+.24;direction=Vector((math.cos(a),math.sin(a),0))
 tree_tube([direction*.02+Vector((0,0,.30)),direction*.40+Vector((0,0,.12)),direction*.85+Vector((0,0,.025))],[.16,.10,.006],7)
for i in range(12):
 a=i*2.399963;level=2.15+i*.255;extent=2.55-i*.095
 base=Vector((.11,.06,level));out=Vector((math.cos(a),math.sin(a),0));side=Vector((-out.y,out.x,0))
 points=[base,base+out*(extent*.28)+Vector((0,0,.20)),base+out*(extent*.65)+side*.12+Vector((0,0,.65)),base+out*extent+side*.20+Vector((0,0,1.10))]
 tree_tube(points,[.115-i*.005,.078-i*.003,.034,.004],8)
 for j in range(4):
  t=.35+j*.17;origin=points[1].lerp(points[-1],t)
  orientation=out*.6+side*((-1 if j%2 else 1)*.75)
  tip=origin+orientation*rng.uniform(.65,1.05)+Vector((0,0,rng.uniform(.27,.65)))
  middle=origin.lerp(tip,.52)+Vector((0,0,.07))
  tree_tube([origin,middle,tip],[.024,.012,.002],5)
  twig=(tip-origin).normalized()
  for k in range(11):
   f=.08+k*.084;attachment=origin.lerp(tip,f)
   leaf_direction=orientation*.35+side*((-1 if k%2 else 1)*.75)+Vector((0,0,rng.uniform(-.15,.4)))
   tree_leaf(attachment,leaf_direction,rng.uniform(.29,.48),rng.uniform(.17,.24),rng.uniform(-.65,.65))
# A small upper crown closes the leader without hiding the branch structure.
for i in range(6):
 a=i*2.4;origin=Vector((.25,.13,5.55));tip=origin+Vector((math.cos(a)*.66,math.sin(a)*.66,.5))
 tree_tube([origin,origin.lerp(tip,.55),tip],[.020,.011,.002],5)
 for k in range(8):
  f=.2+k*.1;direction=Vector((math.cos(a+(k%2-.5)*1.4),math.sin(a+(k%2-.5)*1.4),.25))
  tree_leaf(origin.lerp(tip,f),direction,rng.uniform(.30,.43),.21,rng.uniform(-.6,.6))
finish_tree_mesh('Trunk branching bark',wood_vertices,wood_faces,wood_uv,wood)
finish_tree_mesh('Canopy individual leaves',leaf_vertices,leaf_faces,leaf_uv,leaf)
export('Tree',start)
start=set(o.name for o in bpy.context.scene.objects)
ring_mesh('Crystal rocky footing',[(0,0,0,.70),(0,0,.14,.60)],stone,7)
for i in range(7):
 angle=i*2.4;x=math.sin(angle)*.44;y=math.cos(angle)*.44
 h=[2.1,1.3,1.75,.77,1.1,.63,.95][i];r=.17+(i%3)*.035
 lean=Vector((math.sin(angle)*.25,math.cos(angle)*.25,0))
 shard=ring_mesh('Crystal quartz shard',[(x,y,.08,r*.78),(x+lean.x*.3,y+lean.y*.3,h*.55,r),(x+lean.x*.65,y+lean.y*.65,h*.80,r*.88),(x+lean.x,y+lean.y,h,.008)],glow,6,angle)
 bevel_edges(shard,.010)
export('Crystal',start)
start=set(o.name for o in bpy.context.scene.objects)
bevel_edges(ring_mesh('Base worn stone',[(0,0,0,.44),(0,0,.10,.49),(.02,0,.23,.40),(.02,0,.29,.35)],stone,8),.012)
bevel_edges(cone('Pillar',(0,0,.84),.19,.14,1.1,stone,16),.008)
bevel_edges(cone('Pillar collar',(0,0,1.30),.23,.23,.10,brass,16),.008)
bevel_edges(cone('Bowl',(0,0,1.43),.24,.36,.17,brass,16),.008)
sphere('Light',(0,0,1.72),(.17,.17,.25),glow)
for i in range(4):
 a=math.pi/4+i*math.pi/2;x=math.cos(a)*.28;y=math.sin(a)*.28
 beam('Gold lantern cage',(x,y,1.48),(x*.82,y*.82,1.98),.022,.018,brass,5)
bevel_edges(cone('Gold lantern roof',(0,0,2.01),.37,.12,.20,brass,16),.008)
sphere('Gold lantern finial',(0,0,2.16),(.065,.065,.09),brass)
export('Lantern',start)
# Source scene lays the authored models side-by-side for later editing.
for index,objects in enumerate(groups):
 for o in objects:o.hide_set(False);o.location.x+=index*7
bpy.ops.object.select_all(action='DESELECT')
for collection in list(bpy.data.collections):
 if not collection.objects and not collection.children:bpy.data.collections.remove(collection)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=Vector((10.5,0,2))
   area.spaces.active.region_3d.view_distance=25
   area.spaces.active.shading.color_type='MATERIAL'
bpy.context.scene['asset_note']='Original Aether models. Source layout only; FBX exports are centered at their ground origin. Z is up in Blender; FBX exports use Y up for Unity.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'ArtSource','Aether.blend'))
print('AETHER_MODELS_EXPORTED')
