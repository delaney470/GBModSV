using Il2CppFemur;
using UnityEngine;

namespace GangBeastsSandevistan;

// Diagnostic baseline: change the game clock only. Never extrapolate ragdoll poses.
public sealed class WorldDilation : IDisposable
{
    private readonly float scale, savedScale, savedStep, savedGameplay;
    private readonly Il2CppGB.Game.TimeManager? timeManager;
    private bool applied, disposed;
    public WorldDilation(Actor actor, float scale)
    {
        if (scale <= 0 || scale > 1) throw new ArgumentOutOfRangeException(nameof(scale));
        this.scale = scale;
        savedScale = Time.timeScale;
        savedStep = Time.fixedDeltaTime;
        timeManager = UnityEngine.Object.FindObjectOfType<Il2CppGB.Game.TimeManager>();
        savedGameplay = timeManager == null ? savedScale : timeManager.GamePlayTime;
    }
    public void Apply()
    {
        applied = true;
        if (timeManager != null) timeManager.GamePlayTime = savedGameplay * scale;
        Time.fixedDeltaTime = savedStep * scale;
        Time.timeScale = savedScale * scale;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (!applied) return;
        if (timeManager != null && Mathf.Approximately(timeManager.GamePlayTime, savedGameplay * scale))
            timeManager.GamePlayTime = savedGameplay;
        // Preserve a pause or clock change made by the game while active.
        if (Mathf.Approximately(Time.timeScale, savedScale * scale)) Time.timeScale = savedScale;
        if (Mathf.Approximately(Time.fixedDeltaTime, savedStep * scale)) Time.fixedDeltaTime = savedStep;
    }
}
