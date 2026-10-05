using UnityEngine;

namespace GangBeastsSandevistan;

// Project world-space body/hand history into the existing IMGUI overlay.
// No added native rendering component, custom shader, texture or material is required.
public sealed class MotionEcho
{
    private readonly Transform[] anchors;
    private readonly Queue<(float time, Vector3[] points)> history = new();
    private float nextSample;
    private static GUIStyle? strokeStyle;
    public int Samples => history.Count;
    public MotionEcho(IEnumerable<Transform> anchors) => this.anchors = anchors.Where(t => t != null).Take(3).ToArray();
    public void Update(float now)
    {
        while (history.Count > 0 && now - history.Peek().time > 0.45f) history.Dequeue();
        if (now < nextSample) return;
        nextSample = now + 0.025f;
        if (anchors.Any(t => t == null)) { history.Clear(); return; }
        history.Enqueue((now, anchors.Select(t => t.position).ToArray()));
        while (history.Count > 20) history.Dequeue();
    }
    public void Draw(Camera camera, float now)
    {
        var samples = history.ToArray();
        if (samples.Length == 0 || anchors.Length == 0) return;
        var oldColour = GUI.color;
        var oldMatrix = GUI.matrix;
        try
        {
            for (int i = 1; i < samples.Length; i++)
            {
                float alpha = Mathf.Clamp01(1 - (now - samples[i].time) / 0.45f);
                for (int anchor = 0; anchor < samples[i].points.Length; anchor++)
                {
                    var a = camera.WorldToScreenPoint(samples[i-1].points[anchor]);
                    var b = camera.WorldToScreenPoint(samples[i].points[anchor]);
                    if (a.z <= 0 || b.z <= 0) continue;
                    var from = new Vector2(a.x, Screen.height - a.y);
                    var to = new Vector2(b.x, Screen.height - b.y);
                    // Avoid a streak across the screen after a teleport or camera cut.
                    if (Vector2.Distance(from, to) > Screen.width * 0.3f) continue;
                    GUI.color = anchor == 0 ? new Color(0,1,1,alpha*0.8f) : new Color(1,0.1f,0.35f,alpha*0.8f);
                    Stroke(from, to, anchor == 0 ? 9 : 5);
                }
            }
            // Pulsing torso ring makes activation visible even while standing still.
            var centre = camera.WorldToScreenPoint(samples[^1].points[0]);
            if (centre.z > 0)
            {
                var c = new Vector2(centre.x, Screen.height-centre.y);
                float radius = 25 + Mathf.Sin(now*14)*4;
                GUI.color = new Color(0,1,1,0.9f);
                for(int i=0;i<20;i++)
                {
                    float a=i*Mathf.PI/10, b=(i+1)*Mathf.PI/10;
                    Stroke(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                           c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,3);
                }
            }
        }
        finally { GUI.color = oldColour; GUI.matrix = oldMatrix; }
    }
    private static void Stroke(Vector2 a, Vector2 b, float width)
    {
        float length=Vector2.Distance(a,b);
        if(length<0.1f) return;
        var matrix=GUI.matrix;
        try
        {
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            if (strokeStyle == null)
            {
                strokeStyle = new GUIStyle(GUI.skin.box);
                strokeStyle.normal.background = Texture2D.whiteTexture;
                strokeStyle.border = new RectOffset(0,0,0,0);
            }
            GUI.Box(new Rect(a.x,a.y-width/2,length,width), GUIContent.none, strokeStyle);
        }
        finally { GUI.matrix=matrix; }
    }
}
