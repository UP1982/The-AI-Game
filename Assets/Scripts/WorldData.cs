using System;
using UnityEngine;
namespace Aether {
[Serializable] public class Resident { public string name,role,personality,goal,greeting,color; public float x,z; }
[Serializable] public class Region { public string name,description,biome,sky,ground,accent,landmark,landmarkName,discovery,weather; public int seed; public Resident[] npcs; }
[Serializable] public class World { public string title,premise,source; public Region[] regions; public string[] events; public int[] visited; }
[Serializable] public class ServiceStatus { public bool ok,hasWorld,keyConfigured; public string model,service; public float limit,reserved,actual; public int requests; }
[Serializable] public class WorldResponse { public World world; public ServiceStatus status; }
[Serializable] public class TalkResponse { public string reply,memory,goal,mood; public ServiceStatus status; }
[Serializable] public class ActionData { public int npc; public string goal,thought; public float x,z; }
[Serializable] public class TickResponse { public string @event,weather; public ActionData[] actions; public ServiceStatus status; }
[Serializable] public class Request { public string prompt,message; public int region,npc; }
[Serializable] public class Session { public string token; public int port; }
[Serializable] public class ErrorData { public string error; }
[Serializable] public class PlayerSave { public int region; public float x,y,z,yaw,pitch; public string[] discoveries; }
public static class Palette {
 public static Color Hex(string s,Color fallback) { Color c; return ColorUtility.TryParseHtmlString(s,out c)?c:fallback; }
}
}
