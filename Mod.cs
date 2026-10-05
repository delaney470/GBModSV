using Il2CppFemur;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

[assembly:MelonInfo(typeof(GangBeastsSandevistan.Mod),"Gang Beasts Sandevistan","0.9.0-beta.1","Delan")]
[assembly:MelonGame("Boneloaf","Gang Beasts")]
namespace GangBeastsSandevistan;

public sealed class Mod:MelonMod
{
    private const float SlowFactor=.2f;
    private sealed class Session
    {
        public Actor Actor=null!;
        public PlayerClock? Clock;
        public MotionEcho? Echo;
        public int? TriggerDevice;
        public float Started;
    }
    private readonly PlayerAbilities abilities=new();
    private readonly Dictionary<int,Session> sessions=new();
    private readonly Dictionary<int,int> deviceOwners=new();
    private readonly ButtonEdges buttons=new();
    private WorldDilation? dilation;
    private float startedGameTime,startedRealTime;
    private Actor? selected; // Keyboard fallback only; gamepads use the game's pairing.
    private string message="Each controller: R3 activates or cancels its paired Beast.";
    private MelonPreferences_Entry<bool> showHud=null!;
    private bool inputErrorReported;

    public override void OnInitializeMelon()
    {
        showHud=MelonPreferences.CreateCategory("Sandevistan").CreateEntry("ShowHUD",true);
        LoggerInstance.Msg("Independent local players: R3 toggles the paired Beast. F7 cycles the keyboard target, F8 toggles it. Three seconds active, six seconds cooldown per Beast.");
    }
    private static bool Usable(Actor? a)=>a!=null&&a.gameObject.activeInHierarchy&&a.initialized&&a.bodyHandeler!=null&&a.actorState!=Actor.ActorState.Dead;
    private static bool HasRemoteHuman(IEnumerable<Actor> actors)=>actors.Any(a=>!a.IsAI&&!a.IsLocal);
    private static List<Actor> LocalActors(IEnumerable<Actor> actors)=>actors.Where(a=>Usable(a)&&a.IsLocal&&!a.IsAI).OrderBy(a=>a.playerID).ThenBy(a=>a.GetInstanceID()).ToList();
    private Session SessionFor(Actor actor)
    {
        int id=actor.GetInstanceID();
        if(!sessions.TryGetValue(id,out var s))sessions[id]=s=new Session{Actor=actor};
        return s;
    }
    private Actor? Owner(Gamepad pad,List<Actor> locals)
    {
        var matches=new List<Actor>();
        foreach(var actor in locals)
        {
            var user=actor.InputPlayer;
            if(!user.valid)continue;
            var devices=user.pairedDevices;
            for(int i=0;i<devices.Count;i++)
                if(devices[i].deviceId==pad.deviceId){matches.Add(actor);break;}
        }
        // Never guess from controller index, playerID, or previous scene ownership.
        if(matches.Count!=1){deviceOwners.Remove(pad.deviceId);return null;}
        var owner=matches[0];int id=owner.GetInstanceID();
        if(!deviceOwners.TryGetValue(pad.deviceId,out int old)||old!=id)
        {
            deviceOwners[pad.deviceId]=id;
            LoggerInstance.Msg($"CONTROLLER OWNER device={pad.deviceId} player={owner.playerID} actor={id}");
        }
        return owner;
    }
    public override void OnUpdate()
    {
        double now=Time.realtimeSinceStartup;
        try
        {
            var actors=Object.FindObjectsOfType<Actor>().ToArray();
            bool remote=HasRemoteHuman(actors);
            if(!Application.isFocused||remote)
            {
                StopAll(now);buttons.Reset();
                if(remote)message="Local play only: remote human detected.";
                return;
            }
            var locals=LocalActors(actors);
            foreach(var actor in locals)SessionFor(actor);
            foreach(var pair in sessions.ToArray())
            {
                var s=pair.Value;
                if(abilities.IsActive(pair.Key)&&(abilities.For(pair.Key).Expired(now)||!Usable(s.Actor)||!s.Actor.IsLocal||s.Actor.IsAI))StopSession(pair.Key,now);
            }
            if(Time.timeScale<=0){StopAll(now);buttons.Reset();return;}
            var connected=new HashSet<int>();
            var pads=Gamepad.all;
            for(int i=0;i<pads.Count;i++)
            {
                var pad=pads[i];connected.Add(pad.deviceId);
                var owner=Owner(pad,locals);
                var edge=buttons.Poll(pad.deviceId,false,pad.rightStickButton.isPressed);
                if(!edge.toggle)continue;
                if(owner==null){message="This controller has no uniquely paired local Beast. Join the match first.";continue;}
                LoggerInstance.Msg($"Gamepad device {pad.deviceId}: player={owner.playerID} R3=true");
                Toggle(owner,now,pad.deviceId);
            }
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                connected.Add(keyboard.deviceId);
                var edge=buttons.Poll(keyboard.deviceId,keyboard.f7Key.isPressed,keyboard.f8Key.isPressed);
                if(edge.cycle)Cycle(locals);
                else if(edge.toggle)
                {
                    if(!Usable(selected))selected=locals.FirstOrDefault();
                    if(selected!=null)Toggle(selected,now,keyboard.deviceId);
                }
            }
            foreach(var pair in sessions.ToArray())
                if(abilities.IsActive(pair.Key)&&pair.Value.TriggerDevice is int device&&!connected.Contains(device))StopSession(pair.Key,now);
            buttons.KeepOnly(connected);
            foreach(var device in deviceOwners.Keys.Where(d=>!connected.Contains(d)).ToArray())deviceOwners.Remove(device);
        }
        catch(Exception error)
        {
            StopAll(now);message="Sandevistan stopped: see MelonLoader log.";
            if(!inputErrorReported){LoggerInstance.Error(error.ToString());inputErrorReported=true;}
        }
    }
    private void Cycle(List<Actor> locals)
    {
        if(locals.Count==0){selected=null;message="Start a local match first.";return;}
        int current=selected==null?-1:locals.FindIndex(a=>a.GetInstanceID()==selected.GetInstanceID());
        selected=locals[(current+1)%locals.Count];
        message=$"Keyboard target: Beast {selected.playerID+1}. F8 activates/cancels this Beast.";
    }
    private void Toggle(Actor actor,double now,int? device)
    {
        int id=actor.GetInstanceID();
        if(abilities.IsActive(id))StopSession(id,now);else StartActor(actor,now,device);
    }
    // Explicit commands used by the optional single-player diagnostic runner.
    private void Start(double now)
    {
        if(!Usable(selected))selected=LocalActors(Object.FindObjectsOfType<Actor>()).FirstOrDefault();
        if(selected!=null)StartActor(selected,now,null);
    }
    private void Stop(double now)=>StopAll(now);
    private void StartActor(Actor actor,double now,int? device)
    {
        if(!Usable(actor)||!actor.IsLocal||actor.IsAI||HasRemoteHuman(Object.FindObjectsOfType<Actor>())||Time.timeScale<=0)return;
        int id=actor.GetInstanceID();var s=SessionFor(actor);
        if(!abilities.For(id).TryStart(now,true)){message=$"Beast {actor.playerID+1} is cooling down.";return;}
        s.TriggerDevice=device;s.Started=Time.realtimeSinceStartup;
        try
        {
            if(dilation==null)
            {
                dilation=new WorldDilation(actor,SlowFactor);dilation.Apply();
                startedGameTime=Time.time;startedRealTime=Time.realtimeSinceStartup;
            }
            s.Clock=new PlayerClock(actor,SlowFactor);s.Clock.Apply();
            NativeLocomotion.Start(actor);
            try
            {
                var bodies=actor.bodyHandeler.GetAllRigidbodies().Where(b=>b!=null).ToArray();
                var torso=bodies.FirstOrDefault(b=>b.name.Contains("chest",StringComparison.OrdinalIgnoreCase))??bodies.FirstOrDefault();
                var anchors=new List<Transform>{torso==null?actor.transform:torso.transform};
                anchors.AddRange(bodies.Where(b=>b.name.Contains("hand",StringComparison.OrdinalIgnoreCase)).Take(2).Select(b=>b.transform));
                s.Echo=new MotionEcho(anchors);
            }
            catch(Exception e){LoggerInstance.Warning("Echo unavailable: "+e.Message);}
            message=$"Beast {actor.playerID+1} active. {abilities.ActiveCount} active player(s).";
            LoggerInstance.Msg($"SANDE START player={actor.playerID} actor={id} device={device} active={abilities.ActiveCount} world={Time.timeScale:F3}");
        }
        catch{StopSession(id,now);throw;}
    }
    private void StopSession(int id,double now)
    {
        if(!sessions.TryGetValue(id,out var s))return;
        bool active=abilities.IsActive(id);abilities.For(id).Stop(now);
        foreach(var record in NativeLocomotion.Stop(id))LoggerInstance.Msg(record);
        try{s.Clock?.Dispose();}catch(Exception e){LoggerInstance.Error("Body cleanup: "+e);}finally{s.Clock=null;s.Echo=null;s.TriggerDevice=null;}
        if(active)LoggerInstance.Msg($"SANDE END actor={id} real={Time.realtimeSinceStartup-s.Started:F3}s remaining={abilities.ActiveCount} cooldown=6s");
        if(abilities.ActiveCount==0&&dilation!=null)
        {
            float elapsed=Time.realtimeSinceStartup-startedRealTime;
            LoggerInstance.Msg($"World clock evidence: real={elapsed:F3}s, game={Time.time-startedGameTime:F3}s, ratio={(elapsed>0?(Time.time-startedGameTime)/elapsed:0):F3}, scale={Time.timeScale:F3}");
            try{dilation.Dispose();}finally{dilation=null;}
            LoggerInstance.Msg($"World restored: scale={Time.timeScale:F3}");
        }
    }
    private void StopAll(double now)
    {
        foreach(var pair in sessions.ToArray())if(abilities.IsActive(pair.Key)||pair.Value.Clock!=null)StopSession(pair.Key,now);
        if(abilities.ActiveCount==0&&dilation!=null){try{dilation.Dispose();}finally{dilation=null;}}
    }
    public override void OnFixedUpdate()
    {
        foreach(var pair in sessions.ToArray())
            if(abilities.IsActive(pair.Key))try{pair.Value.Clock?.FixedUpdate();}catch(Exception e){StopSession(pair.Key,Time.realtimeSinceStartup);LoggerInstance.Error(e.ToString());}
    }
    public override void OnLateUpdate()
    {
        foreach(var pair in sessions.ToArray())
        {
            if(!abilities.IsActive(pair.Key))continue;
            var s=pair.Value;
            try
            {
                if(s.Clock?.ExcessiveSpeed()==true){LoggerInstance.Error($"Player clock safety stop: actor={pair.Key} excessive real body speed.");StopSession(pair.Key,Time.realtimeSinceStartup);continue;}
                s.Echo?.Update(Time.realtimeSinceStartup);
            }
            catch(Exception e){StopSession(pair.Key,Time.realtimeSinceStartup);LoggerInstance.Error(e.ToString());}
        }
    }
    private void ResetScene()
    {
        StopAll(Time.realtimeSinceStartup);sessions.Clear();abilities.Reset();deviceOwners.Clear();buttons.Reset();selected=null;inputErrorReported=false;
        message="Each controller: R3 activates or cancels its paired Beast.";
    }
    public override void OnSceneWasUnloaded(int index,string name)=>ResetScene();
    public override void OnSceneWasInitialized(int index,string name){ResetScene();try{PlayerClock.Prepare();}catch(Exception e){LoggerInstance.Error("Player clock preparation failed: "+e);}}
    public override void OnDeinitializeMelon(){ResetScene();PlayerClock.Unload();}
    public override void OnGUI()
    {
        float now=Time.realtimeSinceStartup;var camera=Camera.main;
        foreach(var pair in sessions)
            if(abilities.IsActive(pair.Key)&&pair.Value.Echo!=null&&camera!=null)
                try{pair.Value.Echo!.Draw(camera,now);}catch(Exception e){pair.Value.Echo=null;LoggerInstance.Warning("Echo draw: "+e.Message);}
        if(showHud==null||!showHud.Value)return;
        var visible=sessions.Where(p=>Usable(p.Value.Actor)).OrderBy(p=>p.Value.Actor.playerID).ToArray();
        var lines=new List<string>{"SANDEVISTAN | Independent local players","R3: your ability   F7/F8: keyboard target"};
        foreach(var p in visible)
        {
            var a=abilities.For(p.Key);var actor=p.Value.Actor;
            string status=a.Active?$"ACTIVE {Math.Max(0,a.EndsAt-now):0.0}s":now<a.ReadyAt?$"Cooldown {a.ReadyAt-now:0.0}s":"Ready";
            string target=selected!=null&&selected.GetInstanceID()==p.Key?" [keyboard]":"";
            lines.Add($"Beast {actor.playerID+1}: {status}{target}");
            if(a.Active&&camera!=null)
            {
                var position=camera.WorldToScreenPoint(actor.transform.position+Vector3.up*1.4f);
                if(position.z>0)GUI.Label(new Rect(position.x-60,Screen.height-position.y,180,30),$"▼ SANDEVISTAN P{actor.playerID+1}");
            }
        }
        lines.Add(message);
        GUI.Box(new Rect(16,16,580,26+lines.Count*18),string.Join("\n",lines));
    }
}
