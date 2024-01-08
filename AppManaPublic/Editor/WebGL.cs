using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AppManaPublic.Editor
{
    public class WebGL
    {
        public static void BuildScript() => BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(new BuildPlayerOptions
        {
            extraScriptingDefines = new[] { "UNITY_ASSERTIONS" },
            locationPathName = $"{Application.dataPath}/../build/{BuildTarget.WebGL}/{Application.productName}/",
            scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
            targetGroup = BuildTargetGroup.WebGL,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
    }
}