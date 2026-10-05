import re,json,sys,statistics
from pathlib import Path
text=Path(sys.argv[1]).read_text(encoding='utf-8-sig')
# Never combine results from different attempts or scenes.
starts=list(re.finditer(r'^.*TRIAL baseline-walk .*$',text,re.M))
if starts:text=text[starts[-1].start():]
results={}
for line in text.splitlines():
    m=re.search(r'RESULT (baseline|active)-(walk|punch|jump) (.*)',line)
    if m:
        results[m[1]+'-'+m[2]]={k:float(v) for k,v in re.findall(r'(\w+)=(-?[\d.]+)',m[3])}
checks={}
def ratio(action,key):
    a=results.get('active-'+action,{}).get(key,0)
    b=results.get('baseline-'+action,{}).get(key,0)
    return a/b if b>0 and a>0 else None
ratios={a:ratio(a,k) for a,k in [('walk','walkPath'),('punch','punchWindow'),('jump','jumpFlight')]}
checks['all_six_trials']=len(results)==6
checks['trial_completed_without_abort']='COMPLETE scripted input trials.' in text and 'ABORT ' not in text
checks['walk_faster']=ratios['walk'] is not None and ratios['walk']>=1.1
checks['punch_near_baseline']=ratios['punch'] is not None and .5<=ratios['punch']<=1.3
checks['jump_near_baseline']=ratios['jump'] is not None and .5<=ratios['jump']<=1.35
checks['no_excessive_limb_speed']=bool(results) and all(r['maxRealLimbSpeed']<25 for r in results.values())
checks['jump_height_controlled']=ratio('jump','heightRange') is not None and ratio('jump','heightRange')<=1.5
clock_ratios=[float(m[2]) for m in re.finditer(r'World clock evidence: real=([\d.]+)s, game=[\d.]+s, ratio=([\d.]+)',text) if float(m[1])>=2.9]
checks['full_duration_world_clock']=len(clock_ratios)>=3 and all(.15<=x<=.25 for x in clock_ratios[-3:])
world={}
for name,scale,speed in re.findall(r'WORLD MOTION object=(.*?) scale=([\d.]+) speed=([\d.]+)m/s',text):
    world.setdefault(name,{}).setdefault(scale,[]).append(float(speed))
world_ratios={name:statistics.median(v['0.20'])/statistics.median(v['1.00']) for name,v in world.items() if '0.20' in v and '1.00' in v and statistics.median(v['1.00'])>.1}
report={'checks':checks,'passed':all(checks.values()),'active_to_baseline_ratios':ratios,'results':results,'world_motion_ratios':world_ratios,'limits':'One local arena; no proof of all maps, grabbing, recovery, or combat stability. World transform samples may reflect scripted phases; inspect object identity.'}
print(json.dumps(report,indent=2))
if len(sys.argv)>2:Path(sys.argv[2]).write_text(json.dumps(report,indent=2),encoding='utf-8')
sys.exit(0 if report['passed'] else 1)
