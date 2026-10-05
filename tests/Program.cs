using GangBeastsSandevistan;

int count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); count++;
}
var edges = new ButtonEdges();
// Fail at every body/property boundary, both before and after a setter mutates.
foreach(bool failAfter in new[]{false,true})
for(int failure=0;failure<15;failure++)
{
    var values=Enumerable.Range(1,15).Select(x=>(float)x).ToArray();
    var original=values.ToArray();
    var journal=new MutationJournal();
    try
    {
        for(int index=0;index<values.Length;index++)
        {
            int i=index;float saved=values[i];
            journal.Set(()=>{
                if(i==failure&&!failAfter)throw new InvalidOperationException("injected");
                values[i]*=5;
                if(i==failure&&failAfter)throw new InvalidOperationException("injected");
            },()=>values[i]=saved);
        }
    }
    catch(InvalidOperationException){journal.Rollback();}
    journal.Rollback();
    Check(values.SequenceEqual(original),$"Partial body conversion rolls back at property {failure}, after={failAfter}");
}
{
    var journal=new MutationJournal();int restored=0;
    journal.Set(()=>{},()=>restored++);
    journal.Set(()=>{},()=>throw new InvalidOperationException("destroyed body"));
    journal.Set(()=>{},()=>restored++);
    try{journal.Rollback();}catch(AggregateException){}
    Check(restored==2,"One restoration failure does not skip other bodies");
}
Check(edges.Poll(1, false, false) == (false, false), "Connecting idle pad does nothing");
Check(edges.Poll(1, true, false) == (true, false), "Cycle input is edge-triggered");
Check(edges.Poll(1, true, false) == (false, false), "Held cycle input does not repeat");
edges.Poll(1, false, false);
Check(edges.Poll(1, true, false) == (true, false), "Cycle input works after release");
edges.Poll(1, false, false);
Check(edges.Poll(1, false, true) == (false, true), "R3 toggles");
Check(edges.Poll(1, false, true) == (false, false), "Held R3 cannot immediately cancel");
Check(edges.Poll(2, true, true) == (false, false), "Hotplug while held is suppressed");
Check(edges.Poll(2, true, true) == (false, false), "Hotplug stays suppressed until release");
edges.Poll(2, false, false);
Check(edges.Poll(2, false, true) == (false, true), "Second controller works after release");
Check(edges.Poll(1, false, true) == (false, false), "Controller states are independent");
edges.KeepOnly(new HashSet<int> { 1 });
Check(edges.Poll(2, false, true) == (false, false), "Reconnect cannot synthesize activation");
edges.Reset();
Check(edges.Poll(1, true, true) == (false, false), "Scene or focus reset suppresses held buttons");
edges.Poll(1, false, false);
Check(edges.Poll(1, true, true) == (true, true), "Simultaneous buttons reach explicit action arbitration");

var state = new AbilityState();
Check(!state.Active, "Initially inactive");
Check(!state.TryStart(0, false), "Safety refusal prevents activation");
Check(state.TryStart(10, true), "Ready ability activates");
Check(state.Active && state.EndsAt == 13, "Duration is three real seconds");
Check(!state.TryStart(11, true), "Active ability cannot stack");
Check(!state.Expired(12.99), "Does not expire early");
Check(state.Expired(13), "Expires at exact duration");
state.Stop(13);
Check(!state.Active && state.ReadyAt == 19, "Normal end starts six-second cooldown");
state.Stop(14);
Check(state.ReadyAt == 19, "Repeated cleanup does not extend cooldown");
Check(!state.TryStart(18.99, true), "Cooldown blocks activation");
Check(state.TryStart(19, true), "Ready at exact cooldown boundary");
state.Stop(19.2);
Check(!state.Active && state.ReadyAt == 25.2, "Early cancel still starts full cooldown");
Check(!state.TryStart(20, true), "Early cancel cannot bypass cooldown");
state.Reset();
Check(!state.Active && state.ReadyAt == 0 && state.EndsAt == 0, "Scene reset clears timer state");
Check(state.TryStart(0, true), "New scene can activate");

var roster=new PlayerAbilities();
var p1=roster.For(10);var p2=roster.For(20);var p3=roster.For(30);
Check(p1.TryStart(0,true),"Player 1 starts independently");
Check(p2.TryStart(2,true),"Player 2 can start while Player 1 is active");
Check(p1.EndsAt==3&&p2.EndsAt==5,"Each three-second window uses its own activation time");
Check(roster.ActiveCount==2,"Two players overlap");
Check(p1.Expired(3)&&!p2.Expired(3),"Player 1 expires without expiring Player 2");
p1.Stop(3);
Check(!roster.IsActive(10)&&roster.IsActive(20)&&roster.ActiveCount==1,"Expired player becomes slow while the other remains fast");
Check(p1.ReadyAt==9&&p2.ReadyAt==0,"Cooldown belongs only to the player who stopped");
Check(!p1.TryStart(4,true),"Player 1 cooldown cannot be bypassed by Player 2 activation");
Check(p3.TryStart(4,true),"Third player has an independent ready state");
p2.Stop(5);
Check(p2.ReadyAt==11&&p3.Active&&p3.EndsAt==7,"Stopping Player 2 preserves Player 3 window");
p3.Stop(6);
Check(roster.ActiveCount==0&&p3.ReadyAt==12,"Last cancellation releases world and starts its own cooldown");
Check(p1.TryStart(9,true)&&!p2.TryStart(9,true),"Player 1 becomes ready before Player 2");
p1.Stop(9.5);p1.Stop(10);
Check(p1.ReadyAt==15.5,"Repeated cleanup does not change that player's cooldown");
Check(p2.TryStart(11,true),"Player 2's cooldown expires independently");
p3.TryStart(12,true);
Check(roster.ActiveCount==2,"New overlap after different cooldowns");
roster.Reset();
Check(roster.ActiveCount==0&&!roster.For(10).Active&&roster.For(10).ReadyAt==0,"Scene reset clears every player's timer");
for(int n=0;n<8;n++)roster.For(n).TryStart(n*.25,true);
Check(roster.ActiveCount==8,"Eight independent player slots are supported");
for(int n=0;n<8;n++)Check(roster.For(n).EndsAt==3+n*.25,"Staggered player deadline "+n);

string root=Path.GetFullPath(args[0]);
string mod=File.ReadAllText(Path.Combine(root,"Mod.cs"));
string native=File.ReadAllText(Path.Combine(root,"NativeLocomotion.cs"));
string clock=File.ReadAllText(Path.Combine(root,"PlayerClock.cs"));
string world=File.ReadAllText(Path.Combine(root,"WorldDilation.cs"));
Check(!mod.Contains("pad.leftStickButton.isPressed")&&mod.Contains("pad.rightStickButton.isPressed"),"Only R3 is required for controller activation");
Check(mod.Contains("keyboard.f7Key.isPressed")&&mod.Contains("keyboard.f8Key.isPressed"),"Keyboard fallback retained");
Check(mod.Contains("user.pairedDevices")&&mod.Contains("matches.Count!=1"),"Controller ownership requires unique actual device pairing");
Check(mod.Contains("if(dilation==null)")&&mod.Contains("abilities.ActiveCount==0&&dilation!=null"),"World clock acquired once and released after last player");
Check(mod.Contains("HasRemoteHuman")&&mod.Contains("!a.IsAI&&!a.IsLocal"),"Remote-human guard retained");
Check(native.Contains("activeActors.Contains(id) ? original*Math.Clamp(SelectedWalkMultiplier,1f,4f) : original"),"Only active players receive locomotion compensation");
Check(native.Contains("SelectedWalkMultiplier { get; set; } = 1.5f"),"Approved movement settings preserved");
Check(clock.Contains("Bodies.Remove(item.body.Pointer)")&&!clock.Contains("Bodies.Clear()"),"Player cleanup does not clear other players' registered bodies");
Check(mod.Contains("s.Echo=new MotionEcho(anchors)"),"Each player owns their own trails");
Check(mod.Contains("OnSceneWasUnloaded")&&mod.Contains("OnDeinitializeMelon"),"Lifecycle cleanup retained");
Check(world.Contains("Time.timeScale = savedScale;")&&world.Contains("Time.fixedDeltaTime = savedStep;"),"World settings restored");
Check(!mod.Contains("AfterPhysics")&&!native.Contains("JointDrive"),"Unsafe pose extrapolation remains excluded");
Console.WriteLine($"{count}/{count} checks passed. Structural and timer tests do not validate gameplay physics.");
