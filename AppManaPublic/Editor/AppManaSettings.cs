#if AR_FOUNDATION
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

namespace AppMana.Editor.XR
{
    [Serializable]
    [XRConfigurationData("AppMana XR", AppManaSettings.settingsKey)]
    public class AppManaSettings : ScriptableObject
    {
        public const string settingsKey = "com.appmana.xr.settings";

        public static AppManaSettings currentSettings =>
            EditorBuildSettings.TryGetConfigObject(settingsKey, out AppManaSettings settings) ? settings : null;

        void Awake()
        {
            // Initialization code here if necessary
        }
    }
}
#endif