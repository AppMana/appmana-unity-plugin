using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR;

namespace AppMana.XR
{
    public class AppManaInputSubsystem : XRInputSubsystem
    {
        internal const string subsystemId = "AppMana-Input";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
        }
    }
}