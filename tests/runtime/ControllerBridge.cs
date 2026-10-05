using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Text.Json;
namespace ActionRunner;
public static class ControllerBridge
{
    private static bool scriptedLeft,scriptedJump;
    private static float scriptedHorizontal;
    public static void Script(float horizontal,bool left,bool jump)
    {
        if(Gamepad.all.Count==0)return;
        pad=Gamepad.all[0];
        if(left!=scriptedLeft){Send("lb",left);scriptedLeft=left;}
        if(jump!=scriptedJump){Send("a",jump);scriptedJump=jump;}
        if(horizontal!=scriptedHorizontal)
        {
            var buffer=UnityEngine.InputSystem.LowLevel.StateEvent.From(pad,out var ev,Unity.Collections.Allocator.Temp);
            try{InputControlExtensions.WriteValueIntoEvent<Vector2>(pad.leftStick,new Vector2(horizontal,0),ev);InputSystem.QueueEvent(ev);}
            finally{buffer.Dispose();}
            scriptedHorizontal=horizontal;
        }
    }
    private static readonly string CommandFile=Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory,"Sandevistan-controller-command.json");
    private static string previous="";
    public static void Initialize(){if(File.Exists(CommandFile))previous=File.ReadAllText(CommandFile);}
    private static float until;
    private static Gamepad? pad;
    private static string held="";
    public static void Update(MelonLogger.Instance log)
    {
        try
        {
            if(held!="" && Time.realtimeSinceStartup>=until){Send(held,false);log.Msg("CONTROLLER release "+held);held="";}
            if(!File.Exists(CommandFile))return;
            string text=File.ReadAllText(CommandFile);
            if(text==previous)return;previous=text;
            using var doc=JsonDocument.Parse(text);
            if(doc.RootElement.TryGetProperty("addPad",out var add)&&add.GetBoolean()){InputSystem.AddDevice("Gamepad",null,null);log.Msg("TEST virtual gamepad added");return;}
            if(doc.RootElement.TryGetProperty("multiTest",out var multi)&&multi.GetBoolean()){MultiTrial.Begin(log);return;}
            if(doc.RootElement.TryGetProperty("stopTests",out var stop)&&stop.GetBoolean()){Runner.StopTests();return;}
            if(doc.RootElement.TryGetProperty("restart",out var restart)&&restart.GetBoolean()){Runner.Restart(doc.RootElement.GetProperty("multiplier").GetSingle(),doc.RootElement.TryGetProperty("repeats",out var repeats)?repeats.GetInt32():1);return;}
            string button=doc.RootElement.GetProperty("button").GetString()!;
            if(Gamepad.all.Count==0){log.Warning("CONTROLLER no connected pad");return;}
            pad=Gamepad.all[doc.RootElement.TryGetProperty("pad",out var index)?index.GetInt32():0];
            if(held!="")Send(held,false);
            Send(button,true);held=button;until=Time.realtimeSinceStartup+doc.RootElement.GetProperty("duration").GetSingle();
            log.Msg($"CONTROLLER inject {button} into {pad.displayName} device={pad.deviceId}");
        }
        catch(Exception e){held="";log.Error("CONTROLLER injection failed: "+e);}
    }
    public static void Pulse(int index,string button)
    {
        if(held!="")Send(held,false);
        pad=Gamepad.all[index];Send(button,true);held=button;until=Time.realtimeSinceStartup+.12f;
    }
    private static void Send(string button,bool down)
    {
        if(pad==null)return;
        UnityEngine.InputSystem.Controls.ButtonControl control=button switch
        {
            "a"=>pad.buttonSouth,"b"=>pad.buttonEast,"start"=>pad.startButton,
            "up"=>pad.dpad.up,"down"=>pad.dpad.down,"left"=>pad.dpad.left,"right"=>pad.dpad.right,
            "l3"=>pad.leftStickButton,"r3"=>pad.rightStickButton,
            "lb"=>pad.leftShoulder,"rb"=>pad.rightShoulder,
            _=>throw new ArgumentException("Unsupported test button")
        };
        var buffer=UnityEngine.InputSystem.LowLevel.StateEvent.From(pad,out var inputEvent,Unity.Collections.Allocator.Temp);
        try
        {
            InputControlExtensions.WriteValueIntoEvent<float>(control,down?1f:0f,inputEvent);
            InputSystem.QueueEvent(inputEvent);
        }
        finally{buffer.Dispose();}
    }
}
