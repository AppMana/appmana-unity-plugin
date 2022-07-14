using UnityEditor;
using UnityEngine.Rendering;

namespace AppManaPublic.Editor
{
    /// <summary>
    /// Configures your project for optimal streaming in AppMana
    /// </summary>
    [InitializeOnLoad]
    public class SetUpProject
    {
        static SetUpProject()
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--generic-virtual-method-iterations=2");
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] {GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12});
            PlayerSettings.assemblyVersionValidation = false;
            PlayerSettings.MTRendering = true;
            PlayerSettings.graphicsJobs = true;
            PlayerSettings.graphicsJobMode = GraphicsJobMode.Native;
        }
    }
}