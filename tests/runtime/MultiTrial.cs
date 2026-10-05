using Il2CppFemur;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using Object=UnityEngine.Object;
namespace ActionRunner;

// Actual queued R3 events; reflection only observes production state.
public static class MultiTrial
{
    private static MelonLogger.Instance? log;
    private static Actor[] actors=Array.Empty<Actor>();
    private static MethodInfo? active;
    private static int step;
    private static float started;
    private static bool running;
    private static int failures;
    private static readonly (float time,int pad,string button,string expected)[] script={
        (.2f,0,"r3",""),(1f,-1,"","10"),
        (1.3f,1,"r3",""),(1.8f,-1,"","11"),
        (2f,0,"r3",""),(2.3f,-1,"","01"),
        (2.5f,0,"r3",""),(2.9f,-1,"","01"),(4.7f,-1,"","00"),
        (8.5f,0,"r3",""),(8.9f,-1,"","10"),
        (9f,1,"r3",""),(9.4f,-1,"","10"),
        (10.6f,1,"r3",""),(11f,-1,"","11"),(11.9f,-1,"","01"),
        (13.9f,-1,"","00")};
    public static void Begin(MelonLogger.Instance logger)
    {
        log=logger;
        if(Gamepad.all.Count<2){log.Error("MULTI ABORT: two controllers required");return;}
        var locals=Object.FindObjectsOfType<Actor>().Where(a=>a.initialized&&a.IsLocal&&!a.IsAI).ToArray();
        actors=Enumerable.Range(0,2).Select(i=>locals.SingleOrDefault(a=>Paired(a,Gamepad.all[i].deviceId))).Where(a=>a!=null).ToArray()!;
        if(actors.Length!=2||actors[0].GetInstanceID()==actors[1].GetInstanceID()){log.Error("MULTI ABORT: no distinct paired Beasts");return;}
        var mod=MelonMod.RegisteredMelons.First(m=>m.GetType().FullName=="GangBeastsSandevistan.Mod");
        active=mod.GetType().Assembly.GetType("GangBeastsSandevistan.PlayerClock")!.GetMethod("IsActive");
        started=Time.realtimeSinceStartup;step=failures=0;running=true;
        log.Msg($"MULTI BEGIN actor0={actors[0].GetInstanceID()} player0={actors[0].playerID} device0={Gamepad.all[0].deviceId} actor1={actors[1].GetInstanceID()} player1={actors[1].playerID} device1={Gamepad.all[1].deviceId}");
    }
    private static bool Paired(Actor actor,int device)
    {
        if(!actor.InputPlayer.valid)return false;
        var devices=actor.InputPlayer.pairedDevices;
        for(int i=0;i<devices.Count;i++)if(devices[i].deviceId==device)return true;
        return false;
    }
    public static void Update()
    {
        if(!running)return;
        if(!Application.isFocused||actors.Any(a=>a==null||a.actorState==Actor.ActorState.Dead||a.actorState==Actor.ActorState.Unconscious))
        {running=false;log!.Error("MULTI ABORT: focus loss/death/KO");return;}
        float elapsed=Time.realtimeSinceStartup-started;
        if(step>=script.Length){running=false;log!.Msg($"MULTI COMPLETE checks=10 failures={failures}");return;}
        var action=script[step];if(elapsed<action.time)return;step++;
        if(action.pad>=0){ControllerBridge.Pulse(action.pad,action.button);log!.Msg($"MULTI INPUT t={elapsed:F3} pad={action.pad} button={action.button}");return;}
        string actual=string.Concat(actors.Select(a=>(bool)active!.Invoke(null,new object[]{a.GetInstanceID()})!?"1":"0"));
        float expectedScale=action.expected=="00"?1:.2f;
        bool pass=actual==action.expected&&Math.Abs(Time.timeScale-expectedScale)<.01f;
        if(!pass)failures++;
        log!.Msg($"MULTI CHECK t={elapsed:F3} expected={action.expected} actual={actual} scale={Time.timeScale:F3} pass={pass}");
    }
}
