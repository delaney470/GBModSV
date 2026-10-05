using HarmonyLib;
using Il2CppFemur;
using UnityEngine;

namespace GangBeastsSandevistan;

// A scoped change to the game's own locomotion parameter. Never modify ragdoll poses.
public static class NativeLocomotion
{
    private static readonly HashSet<int> activeActors=new();
    public static bool Active => activeActors.Count>0;
    public static float SelectedWalkMultiplier { get; set; } = 1.5f;

    private static readonly Dictionary<int, (bool ai, int calls, float before, float during)> evidence = new();
    public static void Start(Actor selected) { activeActors.Add(selected.GetInstanceID()); }
    public static IEnumerable<string> Stop(int actorId)
    {
        activeActors.Remove(actorId);
        var result=evidence.Where(x=>x.Key==actorId).Select(x=>$"Native locomotion: actor={x.Key}, ai={x.Value.ai}, calls={x.Value.calls}, baseline={x.Value.before:F3}, effective={x.Value.during:F3}").ToArray();
        evidence.Remove(actorId);
        if(activeActors.Count==0)evidence.Clear();
        return result;
    }
    [HarmonyPatch(typeof(MovementHandeler_HumanoidMediumEctomorph), nameof(MovementHandeler_HumanoidMediumEctomorph.RunCycleRotateBall))]
    private static class SpeedPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(MovementHandeler_HumanoidMediumEctomorph __instance, out float? __state)
        {
            __state=null;
            if (!Active || __instance.actor==null) return;
            var actor=__instance.actor;
            float original=__instance._cycleModifer;
            if (!float.IsFinite(original) || original<0) return;
            int id=actor.GetInstanceID();
            float effective=activeActors.Contains(id) ? original*Math.Clamp(SelectedWalkMultiplier,1f,4f) : original;
            __state=original;
            __instance._cycleModifer=effective;
            evidence.TryGetValue(id,out var previous);
            evidence[id]=(actor.IsAI,previous.calls+1,original,effective);
        }
        private static void Postfix(MovementHandeler_HumanoidMediumEctomorph __instance, float? __state)
        {
            if(__state.HasValue)__instance._cycleModifer=__state.Value;
        }
        private static Exception? Finalizer(MovementHandeler_HumanoidMediumEctomorph __instance, float? __state, Exception? __exception)
        {
            if(__state.HasValue)__instance._cycleModifer=__state.Value;
            return __exception;
        }
    }
}
