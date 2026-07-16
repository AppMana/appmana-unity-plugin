using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AppManaPublic.Editor
{
    public class WebGL
    {
        public static void BuildScript()
        {
            var output = Path.GetFullPath(
                $"{Application.dataPath}/../build/{BuildTarget.WebGL}/{Application.productName}");
            var previousCompression = PlayerSettings.WebGL.compressionFormat;
            var previousFallback = PlayerSettings.WebGL.decompressionFallback;
            var previousHashedNames = PlayerSettings.WebGL.nameFilesAsHashes;

            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.nameFilesAsHashes = false;

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    locationPathName = output,
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path)
                        .ToArray(),
                    targetGroup = BuildTargetGroup.WebGL,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Unity Web build failed with result {report.summary.result}");
                }

                var buildDirectory = Path.Combine(output, "Build");
                var required = new[] { "data.br", "framework.js.br", "wasm.br" };
                var missing = required.Where(suffix =>
                    !File.Exists(Path.Combine(buildDirectory, $"{Application.productName}.{suffix}"))).ToArray();
                if (missing.Length != 0)
                {
                    throw new BuildFailedException(
                        $"Unity Web build did not produce the AppMana release artifacts: {string.Join(", ", missing)}");
                }
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousCompression;
                PlayerSettings.WebGL.decompressionFallback = previousFallback;
                PlayerSettings.WebGL.nameFilesAsHashes = previousHashedNames;
            }
        }
    }
}
