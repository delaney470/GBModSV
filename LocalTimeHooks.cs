using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using MelonLoader.NativeUtils;
using UnityEngine;
namespace GangBeastsSandevistan;

// Selected Actor-scoped conversion between world-clock and player-clock units.
public static class LocalTimeHooks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate float Scalar();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Force(IntPtr body, ref Vector3 value, ForceMode mode);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void VectorAccess(IntPtr body, ref Vector3 value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void BodyScalarSet(IntPtr body,float value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate float BodyScalarGet(IntPtr body);
    private static NativeHook<Scalar>? delta,fixedDelta;
    private static NativeHook<Force>? force,torque;
    private static NativeHook<Force>? relativeForce,relativeTorque;
    private static NativeHook<BodyScalarSet>? setMaxAngular,setDrag,setAngularDrag;
    private static NativeHook<BodyScalarGet>? getMaxAngular,getDrag,getAngularDrag;
    private static NativeHook<VectorAccess>? getVelocity,setVelocity,getAngular,setAngular;
    private static readonly List<Delegate> callbacks=new();
    private static readonly List<Action> detach=new();
    public static readonly HashSet<IntPtr> Bodies=new();
    [ThreadStatic] public static int ScopeDepth;
    public static float Factor=1;
    public static long ForceCalls,TimeCalls;
    public static long LimitCalls,RelativeCalls;
    private static bool Scoped => ScopeDepth>0 && Factor>1;
    private static bool Applies(IntPtr body)=>Scoped && Bodies.Contains(body);
    private static void Attach<T>(ref NativeHook<T>? field,string name,T callback) where T:Delegate
    {
        var ptr=IL2CPP.il2cpp_resolve_icall(name);
        if(ptr==IntPtr.Zero)throw new InvalidOperationException("Missing native call "+name);
        callbacks.Add(callback);
        var hook=new NativeHook<T>(ptr,Marshal.GetFunctionPointerForDelegate(callback));
        field=hook; hook.Attach();detach.Add(hook.Detach);
    }
    public static void Install()
    {
        try
        {
            Attach<Scalar>(ref delta,"UnityEngine.Time::get_deltaTime()",Delta);
            Attach<Scalar>(ref fixedDelta,"UnityEngine.Time::get_fixedDeltaTime()",FixedDelta);
            Attach<Force>(ref force,"UnityEngine.Rigidbody::AddForce_Injected(UnityEngine.Vector3&,UnityEngine.ForceMode)",AddForce);
            Attach<Force>(ref torque,"UnityEngine.Rigidbody::AddTorque_Injected(UnityEngine.Vector3&,UnityEngine.ForceMode)",AddTorque);
            Attach<Force>(ref relativeForce,"UnityEngine.Rigidbody::AddRelativeForce_Injected(UnityEngine.Vector3&,UnityEngine.ForceMode)",AddRelativeForce);
            Attach<Force>(ref relativeTorque,"UnityEngine.Rigidbody::AddRelativeTorque_Injected(UnityEngine.Vector3&,UnityEngine.ForceMode)",AddRelativeTorque);
            Attach<BodyScalarSet>(ref setMaxAngular,"UnityEngine.Rigidbody::set_maxAngularVelocity(System.Single)",SetMaxAngular);
            Attach<BodyScalarGet>(ref getMaxAngular,"UnityEngine.Rigidbody::get_maxAngularVelocity()",GetMaxAngular);
            Attach<BodyScalarSet>(ref setDrag,"UnityEngine.Rigidbody::set_drag(System.Single)",SetDrag);
            Attach<BodyScalarGet>(ref getDrag,"UnityEngine.Rigidbody::get_drag()",GetDrag);
            Attach<BodyScalarSet>(ref setAngularDrag,"UnityEngine.Rigidbody::set_angularDrag(System.Single)",SetAngularDrag);
            Attach<BodyScalarGet>(ref getAngularDrag,"UnityEngine.Rigidbody::get_angularDrag()",GetAngularDrag);
            Attach<VectorAccess>(ref getVelocity,"UnityEngine.Rigidbody::get_velocity_Injected(UnityEngine.Vector3&)",GetVelocity);
            Attach<VectorAccess>(ref setVelocity,"UnityEngine.Rigidbody::set_velocity_Injected(UnityEngine.Vector3&)",SetVelocity);
            Attach<VectorAccess>(ref getAngular,"UnityEngine.Rigidbody::get_angularVelocity_Injected(UnityEngine.Vector3&)",GetAngular);
            Attach<VectorAccess>(ref setAngular,"UnityEngine.Rigidbody::set_angularVelocity_Injected(UnityEngine.Vector3&)",SetAngular);
        }
        catch{Uninstall();throw;}
    }
    public static void Uninstall(){ScopeDepth=0;Factor=1;Bodies.Clear();for(int i=detach.Count-1;i>=0;i--)detach[i]();detach.Clear();}
    private static float Delta(){float value=delta!.Trampoline();if(Scoped){TimeCalls++;return value*Factor;}return value;}
    private static float FixedDelta(){float value=fixedDelta!.Trampoline();return Scoped?value*Factor:value;}
    public static float ForceFactor(ForceMode mode)=>mode==ForceMode.Impulse||mode==ForceMode.VelocityChange?Factor:Factor*Factor;
    private static void AddForce(IntPtr body,ref Vector3 value,ForceMode mode){var adjusted=value;if(Applies(body)){adjusted*=ForceFactor(mode);ForceCalls++;}force!.Trampoline(body,ref adjusted,mode);}
    private static void AddTorque(IntPtr body,ref Vector3 value,ForceMode mode){var adjusted=value;if(Applies(body)){adjusted*=ForceFactor(mode);ForceCalls++;}torque!.Trampoline(body,ref adjusted,mode);}
    private static void AddRelativeForce(IntPtr body,ref Vector3 value,ForceMode mode){var adjusted=value;if(Applies(body)){adjusted*=ForceFactor(mode);RelativeCalls++;}relativeForce!.Trampoline(body,ref adjusted,mode);}
    private static void AddRelativeTorque(IntPtr body,ref Vector3 value,ForceMode mode){var adjusted=value;if(Applies(body)){adjusted*=ForceFactor(mode);RelativeCalls++;}relativeTorque!.Trampoline(body,ref adjusted,mode);}
    private static void SetMaxAngular(IntPtr body,float value){if(Applies(body)){value*=Factor;LimitCalls++;}setMaxAngular!.Trampoline(body,value);}
    private static float GetMaxAngular(IntPtr body){var value=getMaxAngular!.Trampoline(body);return Applies(body)?value/Factor:value;}
    private static void SetDrag(IntPtr body,float value)=>setDrag!.Trampoline(body,Applies(body)?value*Factor:value);
    private static float GetDrag(IntPtr body){var value=getDrag!.Trampoline(body);return Applies(body)?value/Factor:value;}
    private static void SetAngularDrag(IntPtr body,float value)=>setAngularDrag!.Trampoline(body,Applies(body)?value*Factor:value);
    private static float GetAngularDrag(IntPtr body){var value=getAngularDrag!.Trampoline(body);return Applies(body)?value/Factor:value;}
    private static void GetVelocity(IntPtr body,ref Vector3 value){getVelocity!.Trampoline(body,ref value);if(Applies(body))value/=Factor;}
    private static void SetVelocity(IntPtr body,ref Vector3 value){var adjusted=Applies(body)?value*Factor:value;setVelocity!.Trampoline(body,ref adjusted);}
    private static void GetAngular(IntPtr body,ref Vector3 value){getAngular!.Trampoline(body,ref value);if(Applies(body))value/=Factor;}
    private static void SetAngular(IntPtr body,ref Vector3 value){var adjusted=Applies(body)?value*Factor:value;setAngular!.Trampoline(body,ref adjusted);}
}
