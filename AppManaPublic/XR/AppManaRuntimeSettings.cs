#if CORE_UTILS && AR_FOUNDATION
using System;
using Unity.XR.CoreUtils;

namespace AppMana.XR
{
    [Serializable]
    [ScriptableSettingsPath(AppManaConstants.runtimeSettingsPath)]
    public class AppManaRuntimeSettings : ScriptableSettings<AppManaRuntimeSettings>
    {
    }
}
#endif