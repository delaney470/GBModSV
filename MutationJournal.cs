namespace GangBeastsSandevistan;

// Records undo before a setter runs, including setters which mutate then throw.
internal sealed class MutationJournal
{
    private readonly Stack<Action> undo = new();
    public void Set(Action change, Action restore)
    {
        undo.Push(restore);
        change();
    }
    public void Commit() => undo.Clear();
    public void Rollback()
    {
        var errors = new List<Exception>();
        while (undo.TryPop(out var restore))
            try { restore(); } catch (Exception error) { errors.Add(error); }
        if (errors.Count != 0) throw new AggregateException("Body restoration failed", errors);
    }
}
