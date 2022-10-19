using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AppManaPublic.Editor
{
    /// <summary>
    /// Configures your project for optimal streaming on AppMana
    /// </summary>
    [InitializeOnLoad]
    public class SetUpProject
    {
        static SetUpProject()
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--generic-virtual-method-iterations=2");
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] {GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan});
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.assemblyVersionValidation = false;
            PlayerSettings.MTRendering = true;
            PlayerSettings.graphicsJobs = true;
            PlayerSettings.graphicsJobMode = GraphicsJobMode.Native;
        }
    }
}