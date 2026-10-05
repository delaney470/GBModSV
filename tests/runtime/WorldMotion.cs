using UnityEngine;
using Il2CppFemur;
using MelonLoader;
using Object=UnityEngine.Object;
namespace ActionRunner;
public static class WorldMotion
{
    private sealed class Track
    {
        public Transform Transform=null!; public Vector3 Position; public Quaternion Rotation;
        public string Name="";public double Distance,Angle;
    }
    private static readonly List<Track> tracks=new();
    private static float due,last,scale;
    private static bool mixed;
    public static void Reset(){tracks.Clear();due=0;}
    public static void Sample(MelonLogger.Instance log)
    {
        float now=Time.realtimeSinceStartup;
        if(tracks.Count==0)
        {
            if(now<due)return;due=now+3;
            if(!Object.FindObjectsOfType<Actor>().Any(a=>a.IsLocal&&!a.IsAI))return;
            var selected=new Dictionary<int,Transform>();
            foreach(var body in Object.FindObjectsOfType<Rigidbody>())
                if(body.GetComponentInParent<Actor>()==null)selected[body.transform.GetInstanceID()]=body.transform;
            foreach(var t in Object.FindObjectsOfType<Transform>())
            {
                string n=t.name.ToLowerInvariant();
                if(new[]{"truck","train","wheel","conveyor","elevator","vehicle","moving","rotat"}.Any(n.Contains)&&t.GetComponentInParent<Actor>()==null)
                    selected[t.GetInstanceID()]=t;
            }
            foreach(var t in selected.Values.Take(500))tracks.Add(new Track{Transform=t,Position=t.position,Rotation=t.rotation,Name=t.name});
            last=now;scale=Time.timeScale;mixed=false;
            log.Msg($"WORLD monitoring {tracks.Count} non-Beast bodies/vehicle transforms");return;
        }
        if(Math.Abs(Time.timeScale-scale)>.01)mixed=true;
        foreach(var t in tracks)
        {
            if(t.Transform==null)continue;
            Vector3 position=t.Transform.position;Quaternion rotation=t.Transform.rotation;
            t.Distance+=Vector3.Distance(position,t.Position);t.Angle+=Quaternion.Angle(rotation,t.Rotation);
            t.Position=position;t.Rotation=rotation;
        }
        float elapsed=now-last;
        if(elapsed<1)return;
        if(!mixed)
            foreach(var t in tracks.Where(t=>t.Distance>.05||t.Angle>1).OrderByDescending(t=>t.Distance+t.Angle*.01).Take(8))
                log.Msg($"WORLD MOTION object={t.Name} scale={scale:F2} speed={t.Distance/elapsed:F3}m/s rotation={t.Angle/elapsed:F2}deg/s duration={elapsed:F2}s");
        foreach(var t in tracks){t.Distance=0;t.Angle=0;}
        last=now;scale=Time.timeScale;mixed=false;
    }
}
