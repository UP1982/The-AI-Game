import copy, importlib.util, io, json, pathlib, tempfile, types, unittest
from unittest.mock import patch
spec=importlib.util.spec_from_file_location('director',pathlib.Path(__file__).resolve().parents[1]/'Backend/world_server.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

def region():
 return {'name':'Grove','description':'A quiet grove','biome':'grove','sky':'#9AB8AA','ground':'#445544','accent':'#CCAABB','seed':1,'landmark':'observatory','landmarkName':'Old tower','discovery':'An old lens','weather':'clear','npcs':[{'name':'Iris '+str(i),'role':'scholar','personality':'curious','goal':'Find a lens','greeting':'Hello','x':-4,'z':-8,'color':'#AABBCC'} for i in range(3)]}
def world():
 return {'title':'Existing','premise':'An ocean of clouds','regions':[region() for _ in range(3)],'source':'OpenAI','events':[],'visited':[]}
def response(value,usage=None):
 return {'status':'completed','usage':usage or {'input_tokens':15,'output_tokens':20},'output':[{'content':[{'type':'output_text','text':json.dumps(value)}]}]}
class DirectorTests(unittest.TestCase):
 def setUp(self): self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
 def director(self,transport=None):return m.Director(self.temp.name,transport=transport)
 def seed(self):
  m.atomic(pathlib.Path(self.temp.name)/'usage.json',{'reserved':.01,'actual':.001,'requests':1})
  m.atomic(pathlib.Path(self.temp.name)/'world.json',world())
 def test_budget_survives_restart_and_blocks_before_network(self):
  calls=[]
  def fail(p):calls.append(1);raise RuntimeError('network timeout')
  d=self.director(fail)
  for _ in range(1000):
   try:d.generate(m.TALK,'test',{},650)
   except RuntimeError:pass
  self.assertLessEqual(d.ledger['reserved'],m.LIMIT);count=len(calls)
  d=self.director(fail)
  with self.assertRaises(RuntimeError):d.generate(m.TALK,'test',{},650)
  self.assertEqual(count,len(calls))
 def test_atomic_write_keeps_previous_data_on_serialization_failure(self):
  path=pathlib.Path(self.temp.name)/'session.json';m.atomic(path,{'token':'secret'})
  self.assertEqual(path.stat().st_mode & 0o777,0o600)
  with self.assertRaises(ValueError):m.atomic(path,{'bad':float('nan')})
  self.assertEqual(json.loads(path.read_text()),{'token':'secret'})
  self.assertEqual(len(list(path.parent.iterdir())),1)
 def test_bad_json_never_creates_world(self):
  d=self.director(lambda p:response({'bad':True}))
  with self.assertRaises(ValueError):d.create()
  self.assertFalse((pathlib.Path(self.temp.name)/'world.json').exists())
 def test_malformed_ledger_fails_closed(self):
  path=pathlib.Path(self.temp.name)/'usage.json'
  for bad in ['broken','null','[]','{"reserved":0,"actual":0,"requests":0.5}','{"reserved":-1,"actual":0,"requests":0}']:
   path.write_text(bad)
   with self.assertRaises(ValueError):self.director()
 def test_missing_ledger_with_existing_save_fails_closed(self):
  m.atomic(pathlib.Path(self.temp.name)/'world.json',world())
  with self.assertRaises(ValueError): self.director()
 def test_saved_world_and_conversations_are_validated(self):
  self.seed();path=pathlib.Path(self.temp.name)/'world.json'
  for damaged in [None,[],{'title':'Missing fields'},{**world(),'visited':[19]},{**world(),'visited':[0,0]}]:
   m.atomic(path,damaged)
   with self.assertRaises(ValueError):self.director()
  m.atomic(path,world())
  for damaged in [[],{'19:0':[]},{'0:3':[]},{'0:0':[{'traveler':'Alex'}]}]:
   m.atomic(pathlib.Path(self.temp.name)/'memory.json',damaged)
   with self.assertRaises(ValueError):self.director()
 def test_validation_rejects_nonfinite_and_bad_ids(self):
  for x in [float('nan'),float('inf'),True,31]:
   with self.assertRaises(ValueError):m.validate(x,m.number(-30,30))
 def test_conversation_persists_actual_speech_and_reuses_memory(self):
  calls=[]
  def talk(payload):
   calls.append(json.loads(payload['input']))
   return response({'reply':'I will remember your name, Alex.','memory':'The traveler is named Alex.','goal':'Find the old lens','mood':'curious'})
  self.seed();d=self.director(talk)
  d.talk(0,0,'My name is Alex.');d2=self.director(talk)
  self.assertEqual(d2.memory['0:0'][0]['traveler'],'My name is Alex.')
  self.assertEqual(d2.world['regions'][0]['npcs'][0]['goal'],'Find the old lens')
  d2.talk(0,0,'Do you remember me?')
  self.assertEqual(calls[-1]['memories'][0]['traveler'],'My name is Alex.')
  self.assertGreater(d2.ledger['actual'],0)
 def test_cached_world_costs_nothing_and_snapshot_is_independent(self):
  def fail(p):raise AssertionError('should not generate')
  self.seed();d=self.director(fail);before=copy.deepcopy(d.ledger)
  saved=d.create();self.assertEqual(saved['world']['title'],'Existing');self.assertEqual(d.ledger,before)
  saved['world']['regions'].clear();self.assertEqual(len(d.world['regions']),3)
 def test_color_schema_rejects_unrenderable_descriptions(self):
  for value in ['dusky-l','lime','#00FF','blue']:
   with self.assertRaises(ValueError):m.validate(value,m.COLOR)
  m.validate('#9AB8AA',m.COLOR)
 def test_bad_character_and_theme_cannot_consume_budget(self):
  d=self.director();d.world=world()
  with self.assertRaises(ValueError):d.talk(0,-1,'Hello')
  with self.assertRaises(ValueError):d.create(123)
  self.assertEqual(d.ledger['requests'],0)
 def test_invalid_usage_cannot_reduce_accounted_spend(self):
  d=self.director(lambda p:response({}, {'input_tokens':-999999,'output_tokens':0}))
  with self.assertRaises(RuntimeError):d.generate(m.TALK,'test',{},650)
  self.assertEqual(d.ledger['actual'],0);self.assertGreater(d.ledger['reserved'],0)
  self.assertEqual(self.director().ledger,d.ledger)
 def test_actual_overrun_blocks_subsequent_calls(self):
  calls=[]
  def transport(payload): calls.append(payload);return response({}, {'input_tokens':1000000,'output_tokens':0})
  d=self.director(transport)
  with self.assertRaises(ValueError):d.generate(m.TALK,'test',{},650)
  with self.assertRaises(RuntimeError):d.generate(m.TALK,'test',{},650)
  self.assertEqual(len(calls),1)
 def test_expansion_cap_and_save_failure_preserve_world(self):
  self.seed();d=self.director(lambda p:response(region()));before=copy.deepcopy(d.world)
  real_atomic=m.atomic
  def fail_world(path,value):
   if path.name=='world.json':raise OSError('Disk full')
   real_atomic(path,value)
  with patch.object(m,'atomic',fail_world):
   with self.assertRaises(OSError):d.expand()
  self.assertEqual(d.world,before);self.assertEqual(self.director().world,before)
  d.world['regions']=[region() for _ in range(20)];requests=d.ledger['requests']
  with self.assertRaises(RuntimeError):d.expand()
  self.assertEqual(d.ledger['requests'],requests)
 def test_duplicate_tick_actions_do_not_mutate_world(self):
  event={'event':'The mist arrives','weather':'mist','actions':[{'npc':0,'goal':'Watch stars','x':-4,'z':-8,'thought':'Where are the stars?'} for _ in range(3)]}
  self.seed();d=self.director(lambda p:response(event));before=copy.deepcopy(d.world)
  with self.assertRaises(ValueError):d.tick(0)
  self.assertEqual(d.world,before);self.assertEqual(self.director().world,before)
 def request(self,path,headers=None,body=b'{}'):
  d=self.director(lambda p:self.fail('HTTP session must not generate'))
  h=object.__new__(m.Handler);h.path=path;h.headers={'Host':'127.0.0.1:8766','X-World-Token':'test-token','Content-Length':str(len(body)),**(headers or {})}
  h.server=types.SimpleNamespace(director=d,token='test-token',server_port=8766);h.rfile=io.BytesIO(body)
  output=[];h.send_json=lambda code,data:output.append((code,data));h.do_POST();return output[0]
 def test_session_is_authenticated_read_only_and_identifies_project(self):
  code,value=self.request('/session');self.assertEqual(code,200)
  self.assertEqual(value['rootPath'],str(m.ROOT.resolve()));self.assertEqual(value['storagePath'],str(pathlib.Path(self.temp.name).resolve()))
  self.assertEqual(value['status']['requests'],0);self.assertEqual(list(pathlib.Path(self.temp.name).iterdir()),[])
 def test_local_endpoint_rejects_cross_origin_wrong_host_and_token(self):
  for headers in [{'Origin':'https://evil.example'},{'Host':'evil.example:8766'},{'X-World-Token':'bad'},{'X-World-Token':'\u00e9'}]:
   self.assertEqual(self.request('/session',headers)[0],403)
  self.assertEqual(self.request('/session',{'Transfer-Encoding':'chunked'})[0],400)
if __name__=='__main__':unittest.main()
