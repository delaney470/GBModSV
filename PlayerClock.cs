using HarmonyLib;
using Il2CppFemur;
using UnityEngine;

namespace GangBeastsSandevistan;

// Experimental actor clock. No per-step position extrapolation or joint-drive changes.
public sealed class PlayerClock : IDisposable
{
    private readonly List<(Rigidbody body,float drag,float angularDrag,float maxAngular,bool gravity)> bodies=new();
    private readonly float factor;
    private readonly int actorId;
    private bool applied,disposed;
    private static bool hooksReady;
    private static readonly HashSet<int> activeActors=new();
    public static bool IsActive(int id)=>activeActors.Contains(id);
    public static void Prepare(){if(!hooksReady){LocalTimeHooks.Install();hooksReady=true;}}
    public PlayerClock(Actor actor,float worldScale)
    {
        Prepare();
        factor=1/worldScale;
        actorId=actor.GetInstanceID();
        foreach(var body in actor.bodyHandeler.GetAllRigidbodies())
            if(body!=null && !body.isKinematic)bodies.Add((body,body.drag,body.angularDrag,body.maxAngularVelocity,body.useGravity));
    }
    public void Apply()
    {
        if(disposed)throw new ObjectDisposedException(nameof(PlayerClock));
        if(applied)return;
        var journal=new MutationJournal();
        try
        {
            foreach(var item in bodies)
            {
                var body=item.body;
                var velocity=body.velocity;
                var angular=body.angularVelocity;
                journal.Set(()=>body.velocity=velocity*factor,()=>{if(body!=null)body.velocity=velocity;});
                journal.Set(()=>body.angularVelocity=angular*factor,()=>{if(body!=null)body.angularVelocity=angular;});
                journal.Set(()=>body.drag=item.drag*factor,()=>{if(body!=null)body.drag=item.drag;});
                journal.Set(()=>body.angularDrag=item.angularDrag*factor,()=>{if(body!=null)body.angularDrag=item.angularDrag;});
                journal.Set(()=>body.maxAngularVelocity=item.maxAngular*factor,()=>{if(body!=null)body.maxAngularVelocity=item.maxAngular;});
            }
            foreach(var item in bodies)LocalTimeHooks.Bodies.Add(item.body.Pointer);
            LocalTimeHooks.Factor=factor;
            activeActors.Add(actorId);
            applied=true;
            journal.Commit();
        }
        catch(Exception failure)
        {
            foreach(var item in bodies)LocalTimeHooks.Bodies.Remove(item.body.Pointer);
            activeActors.Remove(actorId);
            if(activeActors.Count==0)LocalTimeHooks.Factor=1;
            try{journal.Rollback();}catch(Exception rollback){throw new AggregateException(failure,rollback);}
            throw;
        }
    }
    public void FixedUpdate()
    {
        if(!applied||disposed)return;
        foreach(var item in bodies)
            if(item.body!=null && item.body.useGravity && !item.body.isKinematic)
                item.body.AddForce(Physics.gravity*(factor*factor-1),ForceMode.Acceleration);
    }
    public bool ExcessiveSpeed()=>bodies.Any(x=>x.body!=null&&x.body.velocity.magnitude*Time.timeScale>25);
    public void Dispose()
    {
        if(disposed)return;disposed=true;activeActors.Remove(actorId);
        foreach(var item in bodies)LocalTimeHooks.Bodies.Remove(item.body.Pointer);
        if(activeActors.Count==0){LocalTimeHooks.Factor=1;LocalTimeHooks.ScopeDepth=0;}
        if(!applied)return;
        var restore=new MutationJournal();
        foreach(var item in bodies)
        {
            if(item.body==null)continue;
            // Queue each property separately so one destroyed body cannot block the rest.
            restore.Set(()=>{},()=>{if(item.body!=null)item.body.velocity/=factor;});
            restore.Set(()=>{},()=>{if(item.body!=null)item.body.angularVelocity/=factor;});
            restore.Set(()=>{},()=>{if(item.body!=null)item.body.drag=item.drag;});
            restore.Set(()=>{},()=>{if(item.body!=null)item.body.angularDrag=item.angularDrag;});
            restore.Set(()=>{},()=>{if(item.body!=null)item.body.maxAngularVelocity=item.maxAngular;});
        }
        restore.Rollback();
    }
    public static void Unload(){if(hooksReady)LocalTimeHooks.Uninstall();hooksReady=false;activeActors.Clear();}
    [HarmonyPatch(typeof(Actor),nameof(Actor.FixedUpdate))]
    private static class FixedScope
    {
        private static void Prefix(Actor __instance,out int __state){__state=LocalTimeHooks.ScopeDepth;LocalTimeHooks.ScopeDepth=IsActive(__instance.GetInstanceID())?1:0;}
        private static Exception? Finalizer(int __state,Exception? __exception){LocalTimeHooks.ScopeDepth=__state;return __exception;}
    }
    [HarmonyPatch(typeof(Actor),nameof(Actor.Update))]
    private static class UpdateScope
    {
        private static void Prefix(Actor __instance,out int __state){__state=LocalTimeHooks.ScopeDepth;LocalTimeHooks.ScopeDepth=IsActive(__instance.GetInstanceID())?1:0;}
        private static Exception? Finalizer(int __state,Exception? __exception){LocalTimeHooks.ScopeDepth=__state;return __exception;}
    }
}
