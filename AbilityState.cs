namespace GangBeastsSandevistan;

// Pure logic shared with the executable checks; time is supplied by Unity's real-time clock.
public sealed class AbilityState
{
    public bool Active { get; private set; }
    public double EndsAt { get; private set; }
    public double ReadyAt { get; private set; }
    public bool TryStart(double now, bool allowed)
    {
        if (Active || !allowed || now < ReadyAt) return false;
        Active = true;
        EndsAt = now + 3;
        return true;
    }
    public void Stop(double now)
    {
        if (!Active) return;
        Active = false;
        ReadyAt = now + 6;
    }
    public bool Expired(double now) => Active && now >= EndsAt;
    public void Reset() { Active = false; EndsAt = ReadyAt = 0; }
}

public sealed class ButtonEdges
{
    private readonly Dictionary<int, (bool left, bool right)> previous = new();
    public (bool cycle, bool toggle) Poll(int device, bool left, bool right)
    {
        bool known = previous.TryGetValue(device, out var old);
        previous[device] = (left, right);
        // A pad connected while a stick is held must be released before it can trigger.
        return known ? (left && !old.left, right && !old.right) : (false, false);
    }
    public void KeepOnly(HashSet<int> connected)
    {
        foreach (int id in previous.Keys.Where(id => !connected.Contains(id)).ToArray()) previous.Remove(id);
    }
    public void Reset() => previous.Clear();
}
