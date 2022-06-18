using UnityEditor;
using UnityEngine.Rendering;

namespace AppManaPublic.Editor
{
    [InitializeOnLoad]
    public class SetUpProject
    {
        static SetUpProject()
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--generic-virtual-method-iterations=2");
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 });
            PlayerSettings.assemblyVersionValidation = false;
        }
    }
}