#if AR_FOUNDATION
using System.Collections.Generic;
using AppMana.XR;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Scripting;

namespace AppMana.Editor.XR
{
    [Preserve]
    class AppManaLoaderMetadata : IXRLoaderMetadata
    {
        public string loaderName { get; set; }
        public string loaderType { get; set; }
        public List<BuildTargetGroup> supportedBuildTargets { get; set; }
    }

    [Preserve]
    class AppManaPackageMetadata : IXRPackageMetadata
    {
        public string packageName { get; set; }
        public string packageId { get; set; }
        public string settingsType { get; set; }
        public List<IXRLoaderMetadata> loaderMetadata { get; set; }
    }

    [Preserve]
    class XRPackage : IXRPackage
    {
        static IXRPackageMetadata s_Metadata = new AppManaPackageMetadata()
        {
            packageName = "AppMana Unity Plugin",
            packageId = "com.appmana.unity.public",
            settingsType = typeof(AppManaSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new AppManaLoaderMetadata()
                {
                    loaderName = "AppMana Loader",
                    loaderType = typeof(AppManaLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>()
                    {
                        BuildTargetGroup.Standalone,
                    }
                },
            }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            if (obj is AppManaSettings settings)
            {
                // Initialize settings here if necessary
                return true;
            }

            return false;
        }
    }

    // Settings class

    [CustomEditor(typeof(AppManaRuntimeSettings))]
    class AppManaRuntimeSettingsEditor : UnityEditor.Editor
    {
    }

    // Editor class for settings
    [CustomEditor(typeof(AppManaSettings))]
    class AppManaSettingsEditor : UnityEditor.Editor
    {
        UnityEditor.Editor m_Editor;

        void OnEnable()
        {
            CreateCachedEditor(AppManaRuntimeSettings.Instance, typeof(AppManaRuntimeSettingsEditor), ref m_Editor);
        }

        public override void OnInspectorGUI()
        {
            m_Editor.OnInspectorGUI();
        }
    }

    // Build processor class
    public class AppManaBuildProcessor : XRBuildHelper<AppManaSettings>
    {
        public override string BuildSettingsKey => AppManaSettings.settingsKey;
    }
}
#endif