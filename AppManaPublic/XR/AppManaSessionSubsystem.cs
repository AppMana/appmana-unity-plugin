#if AR_FOUNDATION
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace AppMana.XR
{
    public class AppManaSessionSubsystem : XRSessionSubsystem
    {
        internal const string subsystemId = "AppMana-Session";

        class AppManaProvider : Provider
        {
            public override void Start()
            {
                
            }

            public override void Stop()
            {
                
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
                base.Update(updateParams);
            }

            public override Promise<SessionAvailability> GetAvailabilityAsync()
            {
                // todo: needs to interact with host
                return Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Installed | SessionAvailability.Supported);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = subsystemId,
                providerType = typeof(AppManaProvider),
                subsystemTypeOverride = typeof(AppManaSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false,
            });
        }
    }
}
#endif