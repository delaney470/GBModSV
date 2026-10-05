using MelonLoader;
using HarmonyLib;
using Il2CppFemur;
using Il2CppGB.NetworkedInput;
using UnityEngine;
using System.Reflection;
using Object=UnityEngine.Object;
[assembly:MelonInfo(typeof(ActionRunner.Runner),"Automated Sandevistan action test","1.0.0","Local testing")]
[assembly:MelonGame("Boneloaf","Gang Beasts")]
namespace ActionRunner;
public sealed class Runner:MelonMod
{
    private static Runner? instance;
    private bool armed;
    private int repeatsRemaining;
    private Actor? actor;
    private int phase=-1;
    private float due,start,walkDistance,punchFirst,punchLast,jumpStart,jumpLand,maxHeight,minHeight,maxSpeed;
    private Vector3 lastPosition;
    private int punchCalls,steps;
    private bool left,wasLeft,jump,wasJump;
    private float horizontal;
    private float nextSample;
    private object? abilityMod;
    private Type? abilityType;
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    private readonly string[] phases={"baseline-walk","baseline-punch","baseline-jump","active-walk","active-punch","active-jump"};
    private bool Running=>phase>=0&&phase<phases.Length&&Time.realtimeSinceStartup>=start;
    private bool Target(InputState state)=>Running&&actor!=null&&actor.inputHandler!=null&&state.Pointer==actor.inputHandler.Pointer;
    public override void OnInitializeMelon(){instance=this;ControllerBridge.Initialize();due=Time.realtimeSinceStartup+15;LoggerInstance.Msg("TEST HELPER IDLE: explicit restart command required for scripted walking, punch and jump. No online players allowed. No direct pose or velocity writes.");}
    private void Call(string method)=>abilityType!.GetMethod(method,Private)!.Invoke(abilityMod,new object[]{(double)Time.realtimeSinceStartup});
    public override void OnUpdate()
    {
        ControllerBridge.Update(LoggerInstance);
        MultiTrial.Update();
        if(!armed)return;
        if(Running && begun) ControllerBridge.Script(horizontal,left,jump);
        if(phase==99)return;
        if(phase>=0 && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true){Abort("Escape pressed");return;}
        if(Time.realtimeSinceStartup<due)return;
        try
        {
            var actors=Object.FindObjectsOfType<Actor>().Where(a=>a.initialized&&a.bodyHandeler!=null).ToArray();
            if(!actors.Any(a=>a.IsLocal&&!a.IsAI))return;
            if(actors.Any(a=>!a.IsAI&&!a.IsLocal)){Abort("remote human detected in gameplay");return;}
            if(actor==null)
            {
                actor=actors.FirstOrDefault(a=>a.IsLocal&&!a.IsAI&&a.controlHandeler!=null&&a.controlHandeler.onGround&&(a.actorState==Actor.ActorState.Stand||a.actorState==Actor.ActorState.Idle));
                if(actor==null)return;
                abilityMod=MelonMod.RegisteredMelons.FirstOrDefault(m=>m.GetType().FullName=="GangBeastsSandevistan.Mod");
                if(abilityMod==null){Abort("ability mod absent");return;}
                abilityType=abilityMod.GetType();
                Call("Stop");abilityType.GetField("selected",Private)!.SetValue(abilityMod,actor);
                phase=0;Begin();return;
            }
            if(!Application.isFocused){Abort("game lost focus; trial invalid");return;}
            if(actor.actorState==Actor.ActorState.Dead||actor.actorState==Actor.ActorState.Unconscious){Abort("selected Beast died or was knocked out; sample invalid");return;}
            if(Time.realtimeSinceStartup-start<3.4f)return;
            End();phase++;
            if(phase==phases.Length){phase=99;LoggerInstance.Msg("COMPLETE scripted input trials. Compare measurements; completion does not imply all scenarios passed.");if(repeatsRemaining>1){repeatsRemaining--;actor=null;phase=-1;begun=false;due=Time.realtimeSinceStartup+7;}else armed=false;return;}
            due=Time.realtimeSinceStartup+6.5f;
            start=due;
        }
        catch(Exception e){Abort(e.ToString());}
    }
    private bool begun;
    public static void Restart(float multiplier,int repeats=1)
    {
        var i=instance;if(i==null)return;
        i.armed=true;i.repeatsRemaining=Math.Clamp(repeats,1,5);
        if(i.abilityMod!=null)i.Call("Stop");
        var mod=MelonMod.RegisteredMelons.First(m=>m.GetType().FullName=="GangBeastsSandevistan.Mod");
        mod.GetType().Assembly.GetType("GangBeastsSandevistan.NativeLocomotion")!.GetProperty("SelectedWalkMultiplier")!.SetValue(null,Math.Clamp(multiplier,1,4));
        ControllerBridge.Script(0,false,false);i.actor=null;i.phase=-1;i.begun=false;i.due=Time.realtimeSinceStartup+7;
        i.LoggerInstance.Msg($"RESTART calibration multiplier={multiplier}");
    }
    public static void StopTests(){var i=instance;if(i==null)return;i.Abort("tests stopped by command");i.armed=false;}
    private void Begin()
    {
        start=Time.realtimeSinceStartup;begun=true;
        walkDistance=0;punchFirst=punchLast=jumpStart=jumpLand=-1;punchCalls=steps=0;maxSpeed=0;
        lastPosition=actor!.bodyHandeler.Chest.PartRigidbody.position;maxHeight=minHeight=lastPosition.y;
        left=wasLeft=jump=wasJump=false;horizontal=0;
        if(phase>=3)Call("Start");
        if(Math.Abs(Time.timeScale-(phase>=3?.2f:1f))>.03f)throw new InvalidOperationException("Phase started with unexpected world clock");
        LoggerInstance.Msg($"TRIAL {phases[phase]} actor={actor.GetInstanceID()} timeScale={Time.timeScale:F3}");
        foreach(var body in actor.bodyHandeler.GetAllRigidbodies())
            if(body!=null)LoggerInstance.Msg($"BODY phase={phases[phase]} name={body.name} mass={body.mass:F3} gravity={body.useGravity} kinematic={body.isKinematic} drag={body.drag:F3} angularDrag={body.angularDrag:F3} maxAngular={body.maxAngularVelocity:F3}");
    }
    private void End()
    {
        if(phase>=3)Call("Stop");
        LoggerInstance.Msg($"RESULT {phases[phase]} steps={steps} walkPath={walkDistance:F3}m punchCalls={punchCalls} punchWindow={(punchFirst<0?-1:punchLast-punchFirst):F3}s jumpFlight={(jumpStart<0?-1:jumpLand<0?-2:jumpLand-jumpStart):F3}s heightRange={maxHeight-minHeight:F3}m maxRealLimbSpeed={maxSpeed:F3}");
        left=jump=false;horizontal=0;ControllerBridge.Script(0,false,false);begun=false;
    }
    private void Abort(string reason){LoggerInstance.Error("ABORT "+reason);armed=false;phase=99;left=jump=false;horizontal=0;ControllerBridge.Script(0,false,false);if(abilityMod!=null)Call("Stop");}
    private void BeforeInput(Actor a)
    {
        if(actor==null||a.Pointer!=actor.Pointer||phase<0||phase>=phases.Length||Time.realtimeSinceStartup<due)return;
        if(!begun)Begin();
        float elapsed=Time.realtimeSinceStartup-start;
        wasLeft=left;wasJump=jump;
        int action=phase%3;
        horizontal=action==0?(elapsed<.8f?.65f:elapsed<1.6f?-.65f:0):0;
        left=action==1&&elapsed>=.2f&&elapsed<.3f;
        jump=action==2&&elapsed>=.2f&&elapsed<.3f;
    }
    public override void OnLateUpdate()
    {
        WorldMotion.Sample(LoggerInstance);
        if(!Running||!begun||actor==null)return;
        var rb=actor.bodyHandeler.Chest.PartRigidbody;
        Vector3 p=rb.position;var d=p-lastPosition;d.y=0;walkDistance+=d.magnitude;lastPosition=p;
        maxHeight=Math.Max(maxHeight,p.y);minHeight=Math.Min(minHeight,p.y);
        foreach(var b in actor.bodyHandeler.GetAllRigidbodies())if(b!=null)maxSpeed=Math.Max(maxSpeed,b.velocity.magnitude*Time.timeScale);
        float now=Time.realtimeSinceStartup;
        if(now>=nextSample)
        {
            nextSample=now+.1f;
            var ball=actor.bodyHandeler.Ball.PartRigidbody;
            var c=actor.controlHandeler;
            LoggerInstance.Msg($"SAMPLE phase={phases[phase]} t={now-start:F3} y={p.y:F3} vy={rb.velocity.y*Time.timeScale:F3} state={actor.actorState} ground={c.onGround} jumpTimer={c.jumpTimer:F3} jumpDelay={c.jumpDelay:F3} ballLimit={ball.maxAngularVelocity:F2} ballDrag={ball.drag:F3}");
        }
        if(actor.actorState==Actor.ActorState.Jump&&jumpStart<0)jumpStart=now;
        if(jumpStart>=0&&jumpLand<0&&now-jumpStart>.1f&&actor.controlHandeler.onGround)jumpLand=now;
        steps++;
        if(maxHeight-minHeight>8 || maxSpeed>25)Abort("excessive height/speed; automatic stop");
    }
    [HarmonyPatch(typeof(Actor),nameof(Actor.FixedUpdate))]
    private static class ActorInput{[HarmonyPriority(HarmonyLib.Priority.First)]private static void Prefix(Actor __instance)=>instance?.BeforeInput(__instance);}
    [HarmonyPatch(typeof(MovementHandeler_HumanoidMediumEctomorph),nameof(MovementHandeler_HumanoidMediumEctomorph.ArmActionPunching))]
    private static class Punch{private static void Prefix(MovementHandeler_HumanoidMediumEctomorph __instance){var i=instance;if(i?.Running!=true||i.actor==null||__instance.actor==null||i.actor.Pointer!=__instance.actor.Pointer)return;float now=Time.realtimeSinceStartup;if(i.punchFirst<0)i.punchFirst=now;i.punchLast=now;i.punchCalls++;}}
    public override void OnSceneWasUnloaded(int index,string name){WorldMotion.Reset();if(phase>=0&&phase<phases.Length)Abort("scene unloaded during test");actor=null;phase=-1;begun=false;due=Time.realtimeSinceStartup+8;}
}
