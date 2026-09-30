import json,pathlib,urllib.request
root=pathlib.Path(__file__).resolve().parents[1]
session=json.loads((root/'.runtime/session.json').read_text())
local_http=urllib.request.build_opener(urllib.request.ProxyHandler({}))
def post(route,data):
 req=urllib.request.Request('http://127.0.0.1:'+str(session['port'])+route,data=json.dumps(data).encode(),headers={'Content-Type':'application/json','X-World-Token':session['token']})
 with local_http.open(req,timeout=100) as r:return json.load(r)
w=post('/world',{'prompt':'A mysterious world suspended over an ocean of clouds: jade forests, sandstone observatories, and frozen gardens. You can only explore and talk. Warm, curious, a little melancholy.'})
print(json.dumps({'title':w['world']['title'],'regions':[{k:r[k] for k in ['name','biome','landmark']} for r in w['world']['regions']],'status':w['status']},indent=2))
(root/'Documentation/live-world-check.json').write_text(json.dumps({'source':w['world']['source'],'regionCount':len(w['world']['regions']),'status':w['status']},indent=2))

import sys
if '--interactions' in sys.argv:
 before=w['status']['requests']
 again=post('/world',{})
 assert again['status']['requests']==before, 'Cached world unexpectedly charged'
 a=post('/talk',{'region':0,'npc':0,'message':'What are you looking for in this grove?'})
 assert a['reply'] and a['memory']
 b=post('/talk',{'region':0,'npc':0,'message':'How could someone exploring here recognize what you just described?'})
 assert b['reply'] and b['memory']
 t=post('/tick',{'region':0})
 assert sorted(x['npc'] for x in t['actions'])==[0,1,2]
 print(json.dumps({'dialogue':a['reply'],'followUp':b['reply'],'event':t['event'],'status':t['status']},indent=2))
 (root/'Documentation/live-interaction-check.json').write_text(json.dumps({'dialogue':a,'followUp':b,'director':t},indent=2))
 try:
  req=urllib.request.Request('http://127.0.0.1:'+str(session['port'])+'/talk',data=b'{}',headers={'Content-Type':'application/json'})
  local_http.open(req,timeout=10)
  raise AssertionError('Unauthenticated request accepted')
 except urllib.error.HTTPError as e: assert e.code==403
 print('AETHER_LIVE_AI_CHECKS_PASSED')

if '--expand' in sys.argv:
 import re
 expanded=post('/expand',{})
 assert len(expanded['world']['regions'])==len(w['world']['regions'])+1
 new=expanded['world']['regions'][-1]
 for k in ('sky','ground','accent'): assert re.fullmatch(r'#[0-9A-Fa-f]{6}',new[k])
 print(json.dumps({'newRegion':new['name'],'biome':new['biome'],'status':expanded['status']},indent=2))
 (root/'Documentation/live-expansion-check.json').write_text(json.dumps({'newRegion':new['name'],'regionCount':len(expanded['world']['regions']),'status':expanded['status']},indent=2))
 print('AETHER_EXPANSION_CHECK_PASSED')
