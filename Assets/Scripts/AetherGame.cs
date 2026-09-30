using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace Aether {
public class AetherGame : MonoBehaviour {
 public WorldGeometry geometry; public World world; public ServiceStatus status=new ServiceStatus {limit=.25f};
 public int regionIndex; public string rootPath; public bool playing,paused,busy; public string error="";
 AetherRendering rendering; Camera cam; CharacterController controller; Transform body; Session session; float yaw,pitch,fall,clock,saveClock,tickClock;
 bool journal,autoWalk; int talking=-1; string message="",reply="",memory="",theme="An ocean of clouds, ancient observatories and a civilization that remembers the stars.";
 string eventText="The horizon is waiting.",busyText="",notice=""; float noticeUntil; List<string> discoveries=new List<string>();
 GUIStyle small,normal,title,heading,button,input,kicker; Font font; Texture2D white; bool stylesReady; Vector2 journalScroll,dialogueScroll; bool verify,healthChecked;
 const float W=1440,H=810;
 public static string Arg(string name){var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]==name)return a[i+1];return null;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(FindFirstObjectByType<AetherGame>())return;new GameObject("Aether",typeof(AetherGame));}
 void Awake(){
  Application.targetFrameRate=60; QualitySettings.vSyncCount=1; QualitySettings.shadowDistance=85;QualitySettings.antiAliasing=0;
  rootPath=Arg("--project-root")??Directory.GetParent(Application.dataPath).FullName;
  verify=Array.IndexOf(Environment.GetCommandLineArgs(),"--verify-game")>=0;Application.runInBackground=verify;
  cam=Camera.main;if(!cam){var c=new GameObject("Explorer Camera",typeof(Camera),typeof(AudioListener));cam=c.GetComponent<Camera>();c.tag="MainCamera";}
  cam.nearClipPlane=.08f;cam.farClipPlane=450;cam.fieldOfView=68;
  body=new GameObject("Explorer").transform;controller=body.gameObject.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;controller.center=Vector3.up*.9f;controller.stepOffset=.35f;controller.slopeLimit=55;
  cam.transform.SetParent(body);cam.transform.localPosition=Vector3.up*1.68f;
  var sun=new GameObject("Sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=.9f;sun.color=new Color(1,.95f,.86f);sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.shadows=LightShadows.Soft;
  rendering=AetherRendering.Setup(cam,sun);
  gameObject.AddComponent<AetherAmbience>();geometry=gameObject.AddComponent<WorldGeometry>();geometry.Build(WorldGeometry.Preview());rendering.ApplyRegion(geometry.data);gameObject.AddComponent<AetherAtmosphere>();Teleport(new Vector3(0,0,-24),0,0);
  try {session=JsonUtility.FromJson<Session>(File.ReadAllText(Path.Combine(rootPath,".runtime/session.json")));}catch {error="Start the game with Play Aether.command to connect the world service.";}
  StartCoroutine(Health());if(verify)StartCoroutine(Verify());
 }
 IEnumerator Health(){if(session==null){healthChecked=true;yield break;}using(var req=UnityWebRequest.Get("http://127.0.0.1:"+session.port+"/health")){req.timeout=4;yield return req.SendWebRequest();if(req.result==UnityWebRequest.Result.Success){status=JsonUtility.FromJson<ServiceStatus>(req.downloadHandler.text);error="";}else error="The world service is offline. Reopen Play Aether.command.";healthChecked=true;}}
 public IEnumerator Post<T>(string route,Request data,Action<T> success,string label){
  if(busy)yield break;if(session==null){error="The world service is offline. Open Play Aether.command.";yield break;}
  busy=true;busyText=label;error="";
  try {using(var req=new UnityWebRequest("http://127.0.0.1:"+session.port+route,"POST")){
   req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data)));req.downloadHandler=new DownloadHandlerBuffer();req.SetRequestHeader("Content-Type","application/json");req.SetRequestHeader("X-World-Token",session.token);req.timeout=90;
   yield return req.SendWebRequest();
   if(req.result!=UnityWebRequest.Result.Success){error="The world service could not respond. Your saved world is safe.";try{var e=JsonUtility.FromJson<ErrorData>(req.downloadHandler.text);if(!string.IsNullOrEmpty(e.error))error=e.error;}catch{} }
   else {try{success(JsonUtility.FromJson<T>(req.downloadHandler.text));}catch(Exception e){error="The returned world could not be loaded.";Debug.LogException(e);}}
  }}finally{busy=false;busyText="";}
 }
 public void Enter(){if(busy)return;StartCoroutine(Post<WorldResponse>("/world",new Request {prompt=theme},r=>{if(r==null||r.world==null||r.world.regions==null||r.world.regions.Length==0)throw new InvalidDataException("Missing world regions");world=r.world;status=r.status;regionIndex=0;LoadPlayer();LoadRegion(regionIndex,true);playing=true;paused=!Application.isFocused&&!verify;SyncCursor();eventText=world.premise;SavePlayer();},"Imagining your world"));}
 void LoadRegion(int index,bool restore=false){
  index=Mathf.Clamp(index,0,world.regions.Length-1);regionIndex=index;geometry.Build(world.regions[index]);rendering.ApplyRegion(geometry.data);talking=-1;journal=false;autoWalk=false;journalScroll=Vector2.zero;
  Teleport(new Vector3(0,0,-23),0,0); if(restore)RestorePosition();
  eventText=world.regions[index].description;tickClock=0;SavePlayer();
  StartCoroutine(RememberVisit(index,world));
 }
 IEnumerator RememberVisit(int index,World visitedWorld){while(busy)yield return null;if(world!=visitedWorld)yield break;yield return Post<WorldResponse>("/visit",new Request {region=index},r=>{if(world==visitedWorld){world.visited=r.world.visited;status=r.status;}},"Remembering this place");}
 void Travel(int offset){if(busy)return;int target=regionIndex+offset;if(target<0){Notify("This is where your journey began.");return;}
  SavePlayer();if(target>=world.regions.Length){StartCoroutine(Post<WorldResponse>("/expand",new Request(),r=>{world=r.world;status=r.status;LoadRegion(target);SyncCursor();},"Imagining the next horizon"));}
  else LoadRegion(target);
 }
 void Teleport(Vector3 p,float y,float tilt){controller.enabled=false;body.position=new Vector3(p.x,geometry.Height(p.x,p.z)+.3f,p.z);yaw=y;pitch=tilt;body.rotation=Quaternion.Euler(0,yaw,0);cam.transform.localRotation=Quaternion.Euler(pitch,0,0);controller.enabled=true;fall=0;}
 void Lock(bool active){Cursor.lockState=active?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!active;}
 void SyncCursor(){Lock(playing&&!paused&&!journal&&talking<0&&Application.isFocused);}
 void CloseConversation(){talking=-1;SyncCursor();}
 void CloseJournal(){journal=false;SyncCursor();}
 void Resume(){paused=false;SyncCursor();}
 void Notify(string text){notice=text;noticeUntil=Time.time+5;}
 public string SaveFile=>Path.Combine(rootPath,".runtime",verify?"verification-player.json":"player.json");
 bool SavePlayer(){if(world==null||!playing)return false;try{Directory.CreateDirectory(Path.GetDirectoryName(SaveFile));var s=new PlayerSave {region=regionIndex,x=body.position.x,y=body.position.y,z=body.position.z,yaw=yaw,pitch=pitch,discoveries=discoveries.ToArray()};File.WriteAllText(SaveFile+".tmp",JsonUtility.ToJson(s,true));if(File.Exists(SaveFile))File.Replace(SaveFile+".tmp",SaveFile,SaveFile+".bak");else File.Move(SaveFile+".tmp",SaveFile);return true;}catch{Notify("Could not save explorer position.");return false;}}
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 PlayerSave ReadPlayer(){foreach(var path in new[]{SaveFile,SaveFile+".bak"}){try{var s=JsonUtility.FromJson<PlayerSave>(File.ReadAllText(path));if(s!=null&&Finite(s.x)&&Finite(s.z)&&Finite(s.yaw)&&Finite(s.pitch))return s;}catch{}}return null;}
 void LoadPlayer(){var s=ReadPlayer();if(s==null)return;regionIndex=Mathf.Clamp(s.region,0,world.regions.Length-1);discoveries=new List<string>(s.discoveries??new string[0]);}
 void RestorePosition(){var s=ReadPlayer();if(s!=null&&s.region==regionIndex&&Mathf.Abs(s.x)<55&&Mathf.Abs(s.z)<55)Teleport(new Vector3(s.x,0,s.z),s.yaw%360,Mathf.Clamp(s.pitch,-70,75));}
 void OnApplicationQuit(){SavePlayer();}
 void OnApplicationFocus(bool focus){if(!focus&&playing&&!verify){paused=true;autoWalk=false;Lock(false);SavePlayer();}}
 int Nearby(){for(int i=0;i<geometry.residents.Length;i++)if(geometry.residents[i]&&Vector3.Distance(body.position,geometry.residents[i].position)<3.6f)return i;return -1;}
 int NearbySite(){for(int i=0;i<2;i++){float x=i==0?22:-23,z=i==0?26:18;if(Vector3.Distance(body.position,new Vector3(x,geometry.Height(x,z),z))<4.5f)return i;}return -1;}
 string SiteTitle(int site){return site==0?"Eastern Survey Ruin":"Western Wayfarer Well";}
 void RecordDiscovery(string entry){if(!discoveries.Contains(entry)){discoveries.Add(entry);SavePlayer();}Notify("Discovery recorded in your field journal.");journal=true;journalScroll=Vector2.zero;Lock(false);autoWalk=false;}
 void Interact(){if(busy)return;int n=Nearby();if(n>=0){talking=n;dialogueScroll=Vector2.zero;reply=geometry.data.npcs[n].greeting;memory="";message="";Lock(false);autoWalk=false;return;}
  if(Vector3.Distance(body.position,geometry.landmarkPosition)<8){RecordDiscovery(geometry.data.landmarkName+"\n"+geometry.data.discovery);return;}
  int site=NearbySite();if(site>=0){RecordDiscovery(geometry.data.name+" / "+SiteTitle(site)+"\n"+(site==0?geometry.data.description:geometry.data.discovery));return;}
  if(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(36,0))<5)Travel(1);
  else if(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(-36,0))<5)Travel(-1);
 }
 void Talk(){if(busy||talking<0||string.IsNullOrWhiteSpace(message))return;var said=message;var resident=talking;var region=regionIndex;var currentWorld=world;message="";StartCoroutine(Post<TalkResponse>("/talk",new Request{region=region,npc=resident,message=said},r=>{if(world!=currentWorld)return;world.regions[region].npcs[resident].goal=r.goal;status=r.status;if(regionIndex==region&&talking==resident){reply=r.reply;memory=r.memory;dialogueScroll=Vector2.zero;}SavePlayer();},"Listening to the inhabitant"));}
 void Tick(){if(busy)return;tickClock=0;StartCoroutine(Post<TickResponse>("/tick",new Request{region=regionIndex},r=>{geometry.Apply(r);eventText=r.@event;status=r.status;},"The world is thinking"));}
 void Update(){clock+=Time.deltaTime;
  if(!playing){cam.transform.localRotation=Quaternion.Euler(3,Mathf.Sin(clock*.09f)*9,0);return;}
  if(verify)return;
  if(Input.GetKeyDown(KeyCode.Escape)){
   if(paused)Resume();else if(talking>=0&&!busy)CloseConversation();else if(journal)CloseJournal();else {paused=true;SyncCursor();SavePlayer();}autoWalk=false;
  }
  if(Input.GetKeyDown(KeyCode.Tab)&&talking<0&&!paused){journal=!journal;SyncCursor();autoWalk=false;}
  if(paused||journal||talking>=0)return;
  if(Input.GetKeyDown(KeyCode.E))Interact();if(talking>=0||journal)return;
  if(Input.GetKeyDown(KeyCode.Q))autoWalk=!autoWalk;
  if(Cursor.lockState==CursorLockMode.Locked){yaw+=Input.GetAxis("Mouse X")*1.7f;pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*1.5f,-70,75);}
  if(Input.GetKeyDown(KeyCode.LeftArrow))yaw-=20;if(Input.GetKeyDown(KeyCode.RightArrow))yaw+=20;
  body.rotation=Quaternion.Euler(0,yaw,0);cam.transform.localRotation=Quaternion.Euler(pitch,0,0);
  float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);float z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
  if(autoWalk)z=1;var move=Vector3.ClampMagnitude(body.right*x+body.forward*z,1)*(Input.GetKey(KeyCode.LeftShift)?8:4.8f);
  if(controller.isGrounded){fall=-2;if(Input.GetKeyDown(KeyCode.Space))fall=6; }else fall-=18*Time.deltaTime;
  controller.Move((move+Vector3.up*fall)*Time.deltaTime);
  if(Mathf.Abs(body.position.x)>56||Mathf.Abs(body.position.z)>56||body.position.y<-12){Teleport(new Vector3(0,0,-23),0,0);Notify("The horizon folds you gently back to shore.");autoWalk=false;}
  saveClock+=Time.deltaTime;if(saveClock>12){SavePlayer();saveClock=0;}
  tickClock+=Time.deltaTime;if(tickClock>90&&!verify&&!busy&&string.IsNullOrEmpty(error))Tick();
 }
 void Styles(){if(stylesReady)return;stylesReady=true;white=Texture2D.whiteTexture;font=Font.CreateDynamicFontFromOSFont(new[]{"Avenir Next","Helvetica Neue","Arial"},24);
  normal=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true,richText=false};normal.normal.textColor=new Color(.87f,.91f,.89f);
  small=new GUIStyle(normal){fontSize=14};kicker=new GUIStyle(normal){fontSize=13,fontStyle=FontStyle.Bold};kicker.normal.textColor=new Color(.91f,.74f,.43f);
  title=new GUIStyle(normal){fontSize=74,fontStyle=FontStyle.Normal};heading=new GUIStyle(normal){fontSize=32};
  button=new GUIStyle(normal){alignment=TextAnchor.MiddleCenter,fontSize=17,fontStyle=FontStyle.Bold};
  input=new GUIStyle(GUI.skin.textField){font=font,fontSize=18,wordWrap=true,padding=new RectOffset(15,15,12,10)};input.normal.textColor=Color.white;input.focused.textColor=Color.white;input.normal.background=white;input.focused.background=white;
 }
 void Box(float x,float y,float w,float h,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),white);GUI.color=old;}
 void Text(float x,float y,float w,float h,string text,GUIStyle s=null){GUI.Label(new Rect(x,y,w,h),text??"",s??normal);}
 bool Button(float x,float y,float w,string text,bool primary=false,bool enabled=true){bool was=GUI.enabled;GUI.enabled=enabled;var rect=new Rect(x,y,w,46);var hover=rect.Contains(Event.current.mousePosition);Box(x,y,w,46,primary?new Color(.70f,.48f,.23f,hover?1:.93f):new Color(.15f,.25f,.28f,hover?1:.94f));bool click=GUI.Button(rect,text,button);GUI.enabled=was;return click;}
 void Panel(float x,float y,float w,float h){Box(x,y,w,h,new Color(.035f,.09f,.115f,.95f));Box(x,y,3,h,new Color(.80f,.62f,.32f));}
 void OnGUI(){Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/W,Screen.height/H,1));
  if(!playing){
   Box(0,0,620,H,new Color(.025f,.07f,.09f,.92f));Text(70,76,440,24,"A LIVING WORLD / AN OPEN HORIZON",kicker);Text(63,120,500,115,"A E T H E R",title);
   Text(70,258,440,96,"You are the explorer.\nThe world is alive.",heading);
   Text(70,380,425,88,"Walk through places imagined by AI. Meet inhabitants with their own intentions. Leave a memory behind.");
   Text(70,483,450,22,status.hasWorld?"YOUR WORLD IS SAVED":"GIVE THE WORLD A STARTING IDEA",kicker);
   if(!status.hasWorld){GUI.backgroundColor=new Color(.11f,.19f,.22f);theme=GUI.TextArea(new Rect(70,518,455,87),theme,500,input);GUI.backgroundColor=Color.white;}
   else Text(70,522,430,70,"Your places, discoveries and conversations will be waiting.");
   if(Button(70,632,455,busy?busyText+"...":status.hasWorld?"CONTINUE EXPLORING  →":"IMAGINE MY WORLD  →",true,!busy))Enter();
   Text(70,698,450,36,"OpenAI • $0.25 total generation limit • saved locally",small);Text(70,737,470,57,error,small);
   Text(1010,735,360,22,"SCENE PREVIEW / WORLD NOT YET LOADED",kicker);
   return;
  }
  // Compact framing keeps the landscape at the center of play.
  Panel(28,25,385,100);Text(48,37,355,20,"A E T H E R   /   "+(regionIndex+1).ToString("00"),kicker);Text(47,61,345,47,geometry.data.name,heading);
  Panel(1154,25,258,90);Text(1173,37,225,20,busy?"●  "+busyText:"●  OPENAI WORLD",kicker);Text(1173,65,225,30,geometry.data.weather.ToUpper()+"   •   "+status.requests+" AI calls",small);
  Text(505,29,430,25,"W  ·  N  ·  E     /     "+(((yaw%360)+360)%360).ToString("000")+"°",small);
  if(!paused&&!journal&&talking<0){
   Box(W/2-2,H/2-2,4,4,new Color(1,.96f,.84f,.9f));
   string prompt="";int n=Nearby();
   if(n>=0)prompt="E   Speak with "+geometry.data.npcs[n].name;
   else if(Vector3.Distance(body.position,geometry.landmarkPosition)<8)prompt="E   Study "+geometry.data.landmarkName;
   else if(NearbySite()>=0)prompt="E   Study "+SiteTitle(NearbySite());
   else if(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(36,0))<5)prompt="E   Cross into "+(regionIndex+1<world.regions.Length?world.regions[regionIndex+1].name:"an unwritten horizon");
   else if(Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(-36,0))<5)prompt=regionIndex>0?"E   Return to "+world.regions[regionIndex-1].name:"The beginning of your journey";
   if(prompt!=""){Panel(465,595,510,53);Text(486,608,465,35,prompt);}
   Panel(28,638,540,117);Text(48,650,500,18,"THE WORLD AROUND YOU",kicker);Text(48,676,497,75,eventText.Length>215?eventText.Substring(0,212)+"…":eventText,small);
   Box(0,766,W,44,new Color(.025f,.065f,.075f,.8f));Text(30,774,1120,24,"WASD  Move    MOUSE  Look    SHIFT  Wander faster    SPACE  Jump    E  Interact    TAB  Journal    ESC  Pause    Q  Auto-walk",small);
   if(autoWalk)Text(1130,771,290,24,"AUTO-WALK ON  /  Q TO STOP",kicker);
  }
  if(talking>=0&&!paused){
   Panel(350,215,740,445);var n=geometry.data.npcs[talking];Text(382,240,650,23,n.role.ToUpper(),kicker);Text(382,271,660,52,n.name,heading);
   var remembered=string.IsNullOrEmpty(memory)?"Intention: "+n.goal:"Remembered: "+memory;
   float replyHeight=normal.CalcHeight(new GUIContent(reply??""),625),memoryHeight=small.CalcHeight(new GUIContent(remembered),625);
   dialogueScroll=GUI.BeginScrollView(new Rect(382,334,650,163),dialogueScroll,new Rect(0,0,625,Mathf.Max(163,replyHeight+memoryHeight+22)));
   Text(0,0,625,replyHeight+2,reply);Text(0,replyHeight+16,625,memoryHeight+2,remembered,small);GUI.EndScrollView();
   GUI.backgroundColor=new Color(.10f,.19f,.22f);GUI.SetNextControlName("Message");message=GUI.TextField(new Rect(382,512,650,56),message,500,input);GUI.backgroundColor=Color.white;
   if(Button(382,591,470,busy?"Thinking...":"SPEAK",true,!busy&&!string.IsNullOrWhiteSpace(message)))Talk();
   if(Button(868,591,164,"LEAVE",false,!busy))CloseConversation();
  }
  if(journal&&!paused){Panel(287,151,867,531);Text(323,175,780,22,"NOTES FROM AN IMPOSSIBLE PLACE",kicker);Text(323,209,780,50,"Field journal",heading);
   const string journalHint="Walk toward the central landmark and press E to record your first discovery.";
   float descriptionHeight=normal.CalcHeight(new GUIContent(geometry.data.description??""),750),contentHeight=descriptionHeight+26;
   foreach(var entry in discoveries)contentHeight+=normal.CalcHeight(new GUIContent(entry??""),750)+26;
   if(discoveries.Count==0)contentHeight+=normal.CalcHeight(new GUIContent(journalHint),750)+4;
   journalScroll=GUI.BeginScrollView(new Rect(323,282,790,302),journalScroll,new Rect(0,0,750,Mathf.Max(302,contentHeight)));
   Text(0,0,750,descriptionHeight+2,geometry.data.description);float entryY=descriptionHeight+26;
   if(discoveries.Count==0)Text(0,entryY,750,contentHeight-entryY,journalHint);
   foreach(var entry in discoveries){float entryHeight=normal.CalcHeight(new GUIContent(entry??""),750);Text(0,entryY,750,entryHeight+2,entry);entryY+=entryHeight+26;}GUI.EndScrollView();
   Text(323,613,530,30,discoveries.Count+" discoveries  •  "+world.regions.Length+" imagined regions",small);if(Button(921,607,192,"CLOSE JOURNAL"))CloseJournal();
  }
  if(paused){Panel(460,153,520,510);Text(497,204,450,23,"TAKE A BREATH",kicker);Text(497,243,450,52,"The world can wait.",heading);Text(497,310,436,77,"New generation waits while paused. Any request already running can finish. Your journey stays saved.");
   Text(497,396,436,45,"Estimated API usage: $"+status.actual.ToString("F4")+" / $0.25\nBudget reserved: $"+status.reserved.ToString("F4"),small);
   if(Button(497,459,445,"RETURN TO THE WORLD",true))Resume();if(Button(497,515,215,"SAVE JOURNEY")&&SavePlayer())Notify("Journey saved.");
   if(Button(727,515,215,"QUIT")){SavePlayer();Application.Quit();}
   Text(497,577,430,24,rendering.RenderMode,small);
   if(Button(497,605,445,"GRAPHICS: "+rendering.QualityLabel+"  /  CHANGE"))rendering.SetQuality(rendering.QualityLabel.ToLower().Contains("high")?"medium":"high");
  }
  if(!string.IsNullOrEmpty(error)){Panel(398,697,700,68);Text(419,709,657,54,error,small);}
  else if(Time.time<noticeUntil){float noticeY=paused||journal||talking>=0?699:135;Panel(435,noticeY,570,54);Text(455,noticeY+13,530,36,notice,small);}
 }
 IEnumerator VerificationScreenshot(string name){
  for(int frame=0;frame<12;frame++)yield return null;
  yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(rootPath,"Documentation",name+".png"));yield return new WaitForSeconds(.5f);
 }
 IEnumerator Verify(){
  Debug.Log("AETHER_VERIFY_STARTED");Directory.CreateDirectory(Path.Combine(rootPath,"Documentation"));
  while(!healthChecked)yield return null;
  yield return VerificationScreenshot("menu");
  var issues=new List<string>();var sceneStats=new StringBuilder();
  // Verification only reads a previously generated world; never buy a new world or dialogue.
  if(!status.hasWorld)issues.Add("cached world required: "+error);
  else {Enter();while(busy)yield return null;yield return new WaitForSeconds(1);while(busy)yield return null;}
  if(world==null)issues.Add("world missing: "+error);
  else{
   if(world.source!="OpenAI")issues.Add("world provenance");if(world.regions.Length<3)issues.Add("region count");
   foreach(var model in new[]{"Tree","Lantern","Crystal","Wayfarer"})if(Resources.Load<GameObject>("Models/"+model)==null)issues.Add("imported model missing: "+model);
   playing=true;paused=false;Teleport(new Vector3(0,0,-20),0,0);Physics.SyncTransforms();var start=body.position;controller.Move(Vector3.forward*2);if(Vector3.Distance(start,body.position)<1)issues.Add("movement");
   for(int i=0;i<world.regions.Length;i++){
    LoadRegion(i);while(busy)yield return null;yield return new WaitForSeconds(.3f);
    yield return VerificationScreenshot("region-"+i);
    int triangles=0,activeRenderers=0;foreach(var filter in geometry.GetComponentsInChildren<MeshFilter>()){var renderer=filter.GetComponent<MeshRenderer>();if(renderer&&renderer.enabled&&renderer.gameObject.activeInHierarchy){activeRenderers++;if(filter.sharedMesh&&filter.sharedMesh.isReadable)triangles+=filter.sharedMesh.triangles.Length/3;}}
    sceneStats.AppendLine(world.regions[i].name+": "+activeRenderers+" active mesh renderers, "+triangles+" readable mesh triangles");
    var frameTimes=new float[60];for(int sample=0;sample<frameTimes.Length;sample++){yield return null;frameTimes[sample]=Time.unscaledDeltaTime*1000;}Array.Sort(frameTimes);sceneStats.AppendLine("Stationary native sample (60 frames): median "+frameTimes[30].ToString("F1")+" ms; p95 "+frameTimes[57].ToString("F1")+" ms; terrain heightfield excluded from mesh triangle totals.");
    if(world.visited==null||Array.IndexOf(world.visited,i)<0)issues.Add("region visit not remembered: "+i);
    for(int resident=0;resident<geometry.residents.Length;resident++){
     var person=geometry.residents[resident];if(!person){issues.Add("resident missing");continue;}
     var renderers=person.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0){issues.Add("resident mesh missing");continue;}
     var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
     if(bounds.size.y<.8f||bounds.size.y>4)issues.Add("resident height: "+bounds.size.y);
    }
    if(!Physics.Raycast(new Vector3(12,30,12),Vector3.down,100))issues.Add("terrain collision");
    Teleport(new Vector3(-14,0,-8),38,-4);yield return VerificationScreenshot("detail-"+i);
    var routeCheck=System.Diagnostics.Stopwatch.StartNew();var startResident=geometry.residents[0].position;
    var actions=new ActionData[3];for(int n=0;n<3;n++)actions[n]=new ActionData {npc=n,x=8+n*3,z=-3,goal=geometry.data.npcs[n].goal};
    geometry.Apply(new TickResponse {weather=geometry.data.weather,actions=actions});routeCheck.Stop();sceneStats.AppendLine("Local navigation plan: "+routeCheck.ElapsedMilliseconds+" ms (synthetic, no API request)");
    yield return new WaitForSeconds(.7f);if(Vector3.Distance(startResident,geometry.residents[0].position)<.1f)issues.Add("resident navigation in "+geometry.data.name);
   }
   LoadRegion(0);while(busy)yield return null;yield return new WaitForSeconds(.3f);
   Teleport(geometry.residents[0].position+Vector3.back*1.8f,0,0);Interact();
   if(talking!=0||reply!=geometry.data.npcs[0].greeting)issues.Add("greeting interaction");
   yield return VerificationScreenshot("dialogue");
   paused=true;SyncCursor();if(Cursor.lockState==CursorLockMode.Locked)issues.Add("pause cursor");
   yield return VerificationScreenshot("pause");
   Resume();if(talking!=0||Cursor.lockState==CursorLockMode.Locked)issues.Add("resume conversation cursor");CloseConversation();
   discoveries.Clear();
   // Pick a reachable inspection point outside the landmark's central collider and away from residents.
   var inspection=geometry.landmarkPosition+Vector3.back*6;
   for(int candidate=0;candidate<16;candidate++){
    float angle=candidate*Mathf.PI/8;var point=geometry.landmarkPosition+new Vector3(Mathf.Sin(angle)*6,0,Mathf.Cos(angle)*6);bool clear=true;
    foreach(var person in geometry.residents)if(Vector3.Distance(point,person.position)<4.5f)clear=false;
    if(clear){inspection=point;break;}
   }
   Teleport(inspection,0,0);Interact();if(!journal||discoveries.Count!=1)issues.Add("landmark discovery");
   yield return VerificationScreenshot("journal");CloseJournal();Interact();if(discoveries.Count!=1)issues.Add("duplicate discovery");
   paused=true;SyncCursor();Resume();if(!journal||Cursor.lockState==CursorLockMode.Locked)issues.Add("resume journal cursor");CloseJournal();
   Teleport(new Vector3(8,0,-17),37,-12);bool savedOk=SavePlayer();SavePlayer();
   if(!savedOk||!File.Exists(SaveFile))issues.Add("save");
   else {
    var saved=JsonUtility.FromJson<PlayerSave>(File.ReadAllText(SaveFile));
    Teleport(new Vector3(12,0,-10),90,0);discoveries.Clear();LoadPlayer();RestorePosition();
    if(Mathf.Abs(body.position.x-saved.x)>.1f||Mathf.Abs(body.position.z-saved.z)>.1f)issues.Add("restore position");
    if(Mathf.Abs(yaw-saved.yaw)>.1f||Mathf.Abs(pitch-saved.pitch)>.1f)issues.Add("restore orientation");
    if(discoveries.Count!=saved.discoveries.Length||discoveries.Count!=1)issues.Add("restore discoveries");
    File.WriteAllText(SaveFile,"invalid verification save");Teleport(new Vector3(12,0,-10),90,0);RestorePosition();
    if(Mathf.Abs(body.position.x-saved.x)>.1f||Mathf.Abs(body.position.z-saved.z)>.1f)issues.Add("backup recovery");SavePlayer();SavePlayer();
   }
   for(int site=0;site<2;site++){
    float x=site==0?22:-23,z=site==0?26:18;Teleport(new Vector3(x,0,z-3.7f),0,0);CloseJournal();Interact();
    if(!journal||!discoveries.Exists(entry=>entry.Contains(SiteTitle(site))))issues.Add("side ruin discovery: "+site);
    yield return VerificationScreenshot("side-ruin-"+site);CloseJournal();
   }
   Teleport(new Vector3(0,0,-23),0,0);SyncCursor();yield return VerificationScreenshot("runtime-check");
  }
  File.WriteAllText(Path.Combine(rootPath,"Documentation","scene-complexity.txt"),sceneStats.ToString());
  File.WriteAllText(Path.Combine(rootPath,"Documentation","graphics-capabilities.txt"),rendering.HardwareFlags+"\n"+rendering.RenderMode+"\n"+rendering.SkyLabel);
  File.WriteAllText(Path.Combine(rootPath,"Documentation","runtime-check.txt"),issues.Count==0?"AETHER_RUNTIME_CHECKS_PASSED: cached OpenAI world, all saved regions visited, imported models and resident scale, movement, terrain collision, greeting, modal pause/resume, discovery deduplication, atomic explorer save, position/orientation/discoveries restore, backup recovery. Automated verification only; interactive native playtesting remains separate.":string.Join("\n",issues));Application.Quit(issues.Count==0?0:1);
 }
}
}
