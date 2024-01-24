using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;

namespace AppManaPublic.Editor
{
    /// <summary>
    /// Configures your project for optimal streaming on AppMana
    /// </summary>
    [InitializeOnLoad]
    internal class SetUpProject
    {
        static SetUpProject()
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--generic-virtual-method-iterations=2");
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            graphicsApis = new[] { GraphicsDeviceType.Direct3D11 }
                .Concat(graphicsApis.Except(new[] { GraphicsDeviceType.Direct3D11 })).ToArray();
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                graphicsApis);
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.assemblyVersionValidation = false;
            PlayerSettings.MTRendering = true;
            PlayerSettings.graphicsJobs = true;
            PlayerSettings.graphicsJobMode = GraphicsJobMode.Native;
            PlayerSettings.allowUnsafeCode = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        }
    }
}