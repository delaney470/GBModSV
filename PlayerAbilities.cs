namespace GangBeastsSandevistan;

// One real-time clock per Beast. The world is slow while any clock is active.
public sealed class PlayerAbilities
{
    private readonly Dictionary<int,AbilityState> players=new();
    public AbilityState For(int actorId)
    {
        if(!players.TryGetValue(actorId,out var state))players[actorId]=state=new AbilityState();
        return state;
    }
    public int ActiveCount=>players.Values.Count(s=>s.Active);
    public bool IsActive(int actorId)=>players.TryGetValue(actorId,out var s)&&s.Active;
    public void Reset()=>players.Clear();
}
