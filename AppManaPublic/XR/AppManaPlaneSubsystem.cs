#if AR_FOUNDATION
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace AppMana.XR
{
    public class AppManaPlaneSubsystem : XRPlaneSubsystem
    {
        internal const string subsystemId = "AppMana-Plane";

        class SimulationProvider : Provider
        {
            public override void Start()
            {
            }

            public override void Stop()
            {
            }

            public override void Destroy()
            {
            }

            public override TrackableChanges<BoundedPlane> GetChanges(BoundedPlane defaultPlane, Allocator allocator)
            {
                return new TrackableChanges<BoundedPlane>(0, 0, 0, allocator);
            }

            public override PlaneDetectionMode requestedPlaneDetectionMode { get; set; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = subsystemId,
                providerType = typeof(SimulationProvider),
                subsystemTypeOverride = typeof(AppManaPlaneSubsystem),
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = false,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = false
            };

            XRPlaneSubsystemDescriptor.Create(cinfo);
        }
    }
}
#endif