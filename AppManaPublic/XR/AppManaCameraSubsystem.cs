#if AR_FOUNDATION
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace AppMana.XR
{
    public class AppManaCameraSubsystem : XRCameraSubsystem
    {
        internal const string subsystemId = "AppMana-Camera";

        class AppManaProvider : Provider
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var cInfo = new XRCameraSubsystemCinfo
            {
                id = subsystemId,
                providerType = typeof(AppManaProvider),
                subsystemTypeOverride = typeof(AppManaCameraSubsystem),
                supportsCameraConfigurations = true,
                supportsCameraImage = false
            };

            if (!XRCameraSubsystem.Register(cInfo))
                Debug.LogError("Cannot register the camera subsystem");
        }
    }
}
#endif