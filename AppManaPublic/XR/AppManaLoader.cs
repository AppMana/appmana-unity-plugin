#if AR_FOUNDATION
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace AppMana.XR
{
    /// <summary>
    /// Manages the lifecycle of AppMana subsystems.
    /// </summary>
    public class AppManaLoader : XRLoaderHelper
    {
        static List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new();
        static List<XRPlaneSubsystemDescriptor> s_PlaneSubsystemDescriptors = new();
        static List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new();
        static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors = new();


        public override bool Initialize()
        {
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors,
                AppManaSessionSubsystem.subsystemId);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneSubsystemDescriptors,
                AppManaPlaneSubsystem.subsystemId);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors,
                AppManaCameraSubsystem.subsystemId);
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, AppManaInputSubsystem.subsystemId);


            var sessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();

            return sessionSubsystem != null;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();
            DestroySubsystem<XRInputSubsystem>();

            return true;
        }
    }
}
#endif