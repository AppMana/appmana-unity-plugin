using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AppManaPublic.Editor
{
    public static class UnityGitConfigurator
    {
        [MenuItem("Tools/Configure UnityYamlMerge")]
        public static void ConfigureGitForUnity()
        {
            UpdateGitAttributes();
            ConfigureGitConfig();
            Debug.Log("UnityYamlMerge installed in your Git Repository.");
        }


        private static void UpdateGitAttributes()
        {
            var gitattributesPath = ".gitattributes";
            var existingLines = new HashSet<string>();
            if (File.Exists(gitattributesPath))
            {
                existingLines = new HashSet<string>(File.ReadAllLines(gitattributesPath));
            }

            var gitattributesContent = new string[]
            {
                "* text=auto",
                "# Unity files",
                "*.meta -text merge=unityyamlmerge diff",
                "*.unity -text merge=unityyamlmerge diff",
                "*.asset -text merge=unityyamlmerge diff",
                "*.prefab -text merge=unityyamlmerge diff",
                "*.mat -text merge=unityyamlmerge diff",
                "*.anim -text merge=unityyamlmerge diff",
                "*.controller -text merge=unityyamlmerge diff",
                "*.overrideController -text merge=unityyamlmerge diff",
                "*.physicMaterial -text merge=unityyamlmerge diff",
                "*.physicsMaterial2D -text merge=unityyamlmerge diff",
                "*.playable -text merge=unityyamlmerge diff",
                "*.mask -text merge=unityyamlmerge diff",
                "*.brush -text merge=unityyamlmerge diff",
                "*.flare -text merge=unityyamlmerge diff",
                "*.fontsettings -text merge=unityyamlmerge diff",
                "*.guiskin -text merge=unityyamlmerge diff",
                "*.giparams -text merge=unityyamlmerge diff",
                "*.renderTexture -text merge=unityyamlmerge diff",
                "*.spriteatlas -text merge=unityyamlmerge diff",
                "*.terrainlayer -text merge=unityyamlmerge diff",
                "*.mixer -text merge=unityyamlmerge diff",
                "*.shadervariants -text merge=unityyamlmerge diff",
            };

            using (var sw = new StreamWriter(gitattributesPath, true)) // Append to existing file
            {
                foreach (var line in gitattributesContent)
                {
                    if (!existingLines.Contains(line))
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }


        private static void ConfigureGitConfig()
        {
            var unityYamlMergePath = GetUnityYamlMergePath();

            ExecuteGitCommand($"config --local merge.unityyamlmerge.name \"Unity SmartMerge (UnityYamlMerge)\"");
            ExecuteGitCommand(
                $"config --local merge.unityyamlmerge.driver \"'{unityYamlMergePath}' merge -h -p --force %O %B %A %A\"");
            ExecuteGitCommand("config --local merge.unityyamlmerge.recursive binary");
        }

        private static string GetUnityYamlMergePath()
        {
            var editorPath = EditorApplication.applicationPath;
            var editorDirectory = Path.GetDirectoryName(editorPath)!;

            return Application.platform switch
            {
                RuntimePlatform.WindowsEditor => Path.Combine(editorDirectory, "Data", "Tools", "UnityYamlMerge.exe")
                    .Replace("\\", "/"),
                RuntimePlatform.OSXEditor => Path.Combine(editorDirectory, "Contents", "Tools", "UnityYAMLMerge"),
                _ => throw new Exception("Unsupported platform")
            };
        }

        private static void ExecuteGitCommand(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            using var process = Process.Start(startInfo);
            process!.WaitForExit();
        }
    }
}