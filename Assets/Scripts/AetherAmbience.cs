using UnityEngine;
namespace Aether {
// Original synthesized atmosphere: no recordings or third-party audio assets.
public class AetherAmbience : MonoBehaviour {
 const int Rate=22050, Duration=36;
 const float Volume=.28f, FadeSeconds=3.5f;
 readonly AudioClip[] clips=new AudioClip[3];
 readonly AudioSource[] voices=new AudioSource[2];
 WorldGeometry geometry;
 int biome=-1,activeVoice,outgoingVoice=-1;
 float fadeTime;
 bool fading;

 void Start(){
  geometry=GetComponent<WorldGeometry>();
  for(int i=0;i<clips.Length;i++)clips[i]=CreateAtmosphere(i);
  for(int i=0;i<voices.Length;i++){
   var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
   source.loop=true;source.spatialBlend=0;source.volume=0;source.priority=192;
   voices[i]=source;
  }
  BeginFade(BiomeIndex());
 }
 int BiomeIndex(){
  if(!geometry||geometry.data==null)return 0;
  string value=geometry.data.biome;
  return value=="desert"?1:value=="tundra"?2:0;
 }
 void Update(){
  if(voices[0]==null)return;
  if(fading){
   fadeTime+=Time.unscaledDeltaTime;
   float blend=Mathf.Clamp01(fadeTime/FadeSeconds);
   voices[activeVoice].volume=Volume*Mathf.Sin(blend*Mathf.PI*.5f);
   if(outgoingVoice>=0)voices[outgoingVoice].volume=Volume*Mathf.Cos(blend*Mathf.PI*.5f);
   if(blend>=1){
    if(outgoingVoice>=0){voices[outgoingVoice].Stop();voices[outgoingVoice].clip=null;voices[outgoingVoice].volume=0;}
    outgoingVoice=-1;fading=false;
   }
  }
  // Finish the current fade before adopting the latest region: rapid travel
  // never replaces an audible clip or requires a third source.
  if(!fading){int desired=BiomeIndex();if(desired!=biome)BeginFade(desired);}
 }
 void BeginFade(int desired){
  outgoingVoice=biome<0?-1:activeVoice;
  activeVoice=biome<0?0:1-activeVoice;
  var source=voices[activeVoice];source.clip=clips[desired];source.volume=0;
  source.Play();biome=desired;fadeTime=0;fading=true;
 }
 static AudioClip CreateAtmosphere(int kind){
  var samples=new float[Rate*Duration];var random=new System.Random(4321+kind*197);
  float low=0,soft=0;
  for(int i=0;i<samples.Length;i++){
   float t=i/(float)Rate,noise=(float)random.NextDouble()*2-1;
   low=low*.989f+noise*.011f;soft=soft*.91f+noise*.09f;
   float breeze=.65f+.22f*Mathf.Sin(t*.43f)+.13f*Mathf.Sin(t*.79f+1.7f);
   float wind=kind==1?(low*.44f+(soft-low)*.035f)*breeze:
              kind==2?low*.20f*breeze:(low*.27f+(soft-low)*.022f)*breeze;
   // Keep the original distant harmonic bed far below conversation and UI.
   float bed=Mathf.Sin(t*Mathf.PI*220)*.012f+Mathf.Sin(t*Mathf.PI*330)*.006f+Mathf.Sin(t*Mathf.PI*440)*.003f;
   samples[i]=wind+bed;
  }
  if(kind==0){
   // Widely spaced, differently pitched short calls rather than a repeating chirp.
   Bird(samples,5.3f,.48f,1480,550,.022f);Bird(samples,6.15f,.30f,1920,-280,.012f);
   Bird(samples,17.6f,.65f,1210,690,.018f);Bird(samples,28.7f,.39f,1740,360,.014f);
  }else if(kind==2){
   Chime(samples,4.7f,1174.66f,.014f);Chime(samples,16.1f,1567.98f,.010f);
   Chime(samples,28.4f,1318.51f,.012f);
  }
  // Quiet loop edges remove any discontinuity in the generated noise waveform.
  int edge=(int)(Rate*1.4f);
  for(int i=0;i<edge;i++){
   float x=i/(float)edge;float smooth=x*x*(3-2*x);
   samples[i]*=smooth;samples[samples.Length-1-i]*=smooth;
  }
  string name=kind==1?"Dry wind and distant harmonics":kind==2?"Ice chimes and distant harmonics":"Leaves, distant birds and harmonics";
  var clip=AudioClip.Create(name,samples.Length,1,Rate,false);clip.SetData(samples,0);return clip;
 }
 static void Bird(float[] samples,float start,float duration,float frequency,float sweep,float amplitude){
  int offset=(int)(start*Rate),count=(int)(duration*Rate);
  for(int i=0;i<count&&offset+i<samples.Length;i++){
   float t=i/(float)Rate,x=t/duration;
   float envelope=Mathf.Sin(x*Mathf.PI);envelope*=envelope;
   float phase=2*Mathf.PI*(frequency*t+sweep*t*t/(2*duration));
   samples[offset+i]+=amplitude*envelope*(Mathf.Sin(phase)+.16f*Mathf.Sin(phase*2));
  }
 }
 static void Chime(float[] samples,float start,float frequency,float amplitude){
  int offset=(int)(start*Rate),count=Rate*5;
  for(int i=0;i<count&&offset+i<samples.Length;i++){
   float t=i/(float)Rate;
   float envelope=Mathf.Min(t/.09f,1)*Mathf.Exp(-t*1.3f)*Mathf.Clamp01((5-t)/.5f);
   float phase=t*2*Mathf.PI*frequency;
   samples[offset+i]+=amplitude*envelope*(Mathf.Sin(phase)+.24f*Mathf.Sin(phase*2.756f));
  }
 }
 void OnDestroy(){
  for(int i=0;i<voices.Length;i++)if(voices[i]){voices[i].Stop();voices[i].clip=null;}
  for(int i=0;i<clips.Length;i++)if(clips[i])Destroy(clips[i]);
 }
}
}
