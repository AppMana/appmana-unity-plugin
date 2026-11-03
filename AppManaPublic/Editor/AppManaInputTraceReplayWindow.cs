using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AppManaPublic.Editor
{
    /// <summary>
    /// Unity Editor utility window for loading and replaying downloaded AppMana Unity Input System
    /// trace archives (.zip), extracted recording directories (manifest.json + *.inputtrace),
    /// or individual .inputtrace files in the Editor.
    /// </summary>
    public class AppManaInputTraceReplayWindow : EditorWindow
    {
        [Serializable]
        private class RecordedDeviceSummary
        {
            public int deviceId;
            public string name;
            public string layout;
        }

        private string sourcePath = "";
        private string activeDirectory = "";
        private readonly List<string> segmentFiles = new List<string>();
        private readonly List<RecordedDeviceSummary> recordedDevices = new List<RecordedDeviceSummary>();
        private readonly List<InputDevice> createdDevices = new List<InputDevice>();
        private Vector2 scrollPosition;

        // Metadata fields parsed from manifest.json
        private bool hasManifest;
        private bool integrityVerified;
        private string integrityError;
        private string recordingId = "-";
        private string sessionId = "-";
        private string attemptId = "-";
        private string recordedUnityVersion = "-";
        private string recordedInputSystemVersion = "-";
        private string stopReason = "-";
        private double durationSeconds;
        private bool truncated;
        private long totalEventCount;
        private long totalBytes;

        // Playback state
        private bool autoEnterPlayMode = true;
        private bool loopReplay;
        private bool useTimestamps;
        private bool pendingStartOnPlayMode;
        private bool isPlaying;
        private bool isPaused;
        private int currentSegmentIndex;
        private InputEventTrace activeTrace;
        private InputEventTrace.ReplayController activeController;
        private readonly Dictionary<int, InputDevice> activeDeviceMap = new Dictionary<int, InputDevice>();

        [MenuItem("Window/AppMana/Input Trace Replay...")]
        [MenuItem("AppMana/Input Trace Replay...")]
        public static AppManaInputTraceReplayWindow ShowWindow()
        {
            var window = GetWindow<AppManaInputTraceReplayWindow>("Input Trace Replay");
            window.minSize = new Vector2(460, 420);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            StopReplay();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && pendingStartOnPlayMode)
            {
                pendingStartOnPlayMode = false;
                BeginSegmentReplay();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                pendingStartOnPlayMode = false;
                StopReplay();
            }
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (isPlaying)
            {
                Repaint();
            }
        }

        public void LoadFromPath(string path)
        {
            StopReplay();
            sourcePath = path ?? "";
            activeDirectory = "";
            segmentFiles.Clear();
            recordedDevices.Clear();
            hasManifest = false;
            integrityVerified = false;
            integrityError = null;
            recordingId = "-";
            sessionId = "-";
            attemptId = "-";
            recordedUnityVersion = "-";
            recordedInputSystemVersion = "-";
            stopReason = "-";
            durationSeconds = 0;
            truncated = false;
            totalEventCount = 0;
            totalBytes = 0;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            try
            {
                if (File.Exists(sourcePath))
                {
                    var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                    if (ext == ".zip")
                    {
                        var extractRoot = Path.Combine(
                            Path.GetTempPath(),
                            "AppManaInputReplay",
                            Path.GetFileNameWithoutExtension(sourcePath) + "-" + ComputeShortPathHash(sourcePath));
                        if (Directory.Exists(extractRoot))
                        {
                            Directory.Delete(extractRoot, true);
                        }
                        Directory.CreateDirectory(extractRoot);
                        ExtractZipArchive(sourcePath, extractRoot);
                        activeDirectory = extractRoot;
                        LoadDirectory(extractRoot);
                    }
                    else if (ext == ".inputtrace")
                    {
                        activeDirectory = Path.GetDirectoryName(sourcePath) ?? "";
                        segmentFiles.Add(sourcePath);
                        InspectSegments();
                    }
                    else if (Path.GetFileName(sourcePath).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                    {
                        activeDirectory = Path.GetDirectoryName(sourcePath) ?? "";
                        LoadDirectory(activeDirectory);
                    }
                    else
                    {
                        integrityError = "Unsupported file type. Select an input-trace-*.zip archive, manifest.json, or .inputtrace file.";
                    }
                }
                else if (Directory.Exists(sourcePath))
                {
                    activeDirectory = sourcePath;
                    LoadDirectory(sourcePath);
                }
                else
                {
                    integrityError = $"Path does not exist: {sourcePath}";
                }
            }
            catch (Exception ex)
            {
                integrityError = ex.Message;
            }

            Repaint();
        }

        private void LoadDirectory(string directory)
        {
            var manifestPath = Path.Combine(directory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                hasManifest = true;
                var manifest = JObject.Parse(File.ReadAllText(manifestPath));
                var metadata = manifest["metadata"];
                var session = metadata?["session"];
                recordingId = (string)metadata?["recordingId"] ?? "-";
                sessionId = (string)session?["sessionId"] ?? "-";
                attemptId = (string)session?["attemptId"] ?? "-";
                recordedUnityVersion = (string)metadata?["unityVersion"] ?? "-";
                recordedInputSystemVersion = (string)metadata?["inputSystemVersion"] ?? "-";
                stopReason = (string)manifest["stopReason"] ?? "-";
                durationSeconds = (double?)manifest["durationSeconds"] ?? 0;
                truncated = (bool?)manifest["truncated"] ?? false;

                var chunks = manifest["chunks"] as JArray;
                if (chunks != null && chunks.Count > 0)
                {
                    foreach (var chunk in chunks)
                    {
                        VerifySegment(directory, chunk, "input", ".inputtrace");
                        if (chunk["frames"] != null)
                        {
                            VerifySegment(directory, chunk, "frames", ".frames.json");
                        }
                        segmentFiles.Add(Path.Combine(directory, (string)chunk["input"]));
                    }
                    integrityVerified = true;
                }
            }

            if (segmentFiles.Count == 0)
            {
                segmentFiles.AddRange(
                    Directory.GetFiles(directory, "*.inputtrace", SearchOption.TopDirectoryOnly)
                        .OrderBy(p => p, StringComparer.Ordinal));
            }

            InspectSegments();
        }

        private static void VerifySegment(string directory, JToken chunk, string field, string suffix)
        {
            var name = (string)chunk[field];
            if (!Regex.IsMatch(name ?? "", "^[0-9]{6}" + Regex.Escape(suffix) + "$"))
                throw new InvalidDataException($"Invalid recording segment name: {name}");
            var path = Path.Combine(directory, name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing recording segment: {name}", path);
            var expectedSize = (long?)chunk[field + "Bytes"] ?? -1;
            var actualSize = new FileInfo(path).Length;
            if (expectedSize < 0 || actualSize != expectedSize)
                throw new InvalidDataException($"Segment size mismatch for {name}: expected {expectedSize}, got {actualSize}");
            var expectedSha256 = (string)chunk[field + "Sha256"];
            if (!string.IsNullOrEmpty(expectedSha256))
            {
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    var actualSha = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(actualSha, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"SHA-256 mismatch for {name}");
                }
            }
        }

        private void InspectSegments()
        {
            var seenDevices = new HashSet<int>();
            totalEventCount = 0;
            totalBytes = 0;

            foreach (var file in segmentFiles)
            {
                if (!File.Exists(file)) continue;
                totalBytes += new FileInfo(file).Length;
                using (var trace = new InputEventTrace())
                {
                    trace.ReadFrom(file);
                    totalEventCount += trace.eventCount;
                    foreach (var device in trace.deviceInfos)
                    {
                        if (seenDevices.Add(device.deviceId))
                        {
                            recordedDevices.Add(new RecordedDeviceSummary
                            {
                                deviceId = device.deviceId,
                                name = device.name,
                                layout = device.layout
                            });
                        }
                    }
                }
            }
        }

        private void StartReplay()
        {
            if (segmentFiles.Count == 0) return;

            if (!EditorApplication.isPlaying && autoEnterPlayMode)
            {
                pendingStartOnPlayMode = true;
                EditorApplication.isPlaying = true;
                return;
            }

            BeginSegmentReplay();
        }

        private void BeginSegmentReplay()
        {
            StopReplay();
            if (segmentFiles.Count == 0) return;

            ResolveAndMapDevices();
            currentSegmentIndex = 0;
            isPlaying = true;
            isPaused = false;
            PlayNextSegment();
        }

        private void ResolveAndMapDevices()
        {
            activeDeviceMap.Clear();
            var usedTargetIds = new HashSet<int>();

            foreach (var recorded in recordedDevices)
            {
                var target = InputSystem.devices.FirstOrDefault(
                    d => d.added && d.layout == recorded.layout && !usedTargetIds.Contains(d.deviceId));
                if (target == null)
                {
                    target = InputSystem.AddDevice(recorded.layout);
                    createdDevices.Add(target);
                }
                usedTargetIds.Add(target.deviceId);
                activeDeviceMap[recorded.deviceId] = target;
            }
        }

        private void PlayNextSegment()
        {
            activeController?.Dispose();
            activeTrace?.Dispose();
            activeController = null;
            activeTrace = null;

            if (!isPlaying) return;

            if (currentSegmentIndex >= segmentFiles.Count)
            {
                if (loopReplay && segmentFiles.Count > 0)
                {
                    currentSegmentIndex = 0;
                }
                else
                {
                    StopReplay();
                    return;
                }
            }

            var path = segmentFiles[currentSegmentIndex++];
            activeTrace = new InputEventTrace();
            activeTrace.ReadFrom(path);
            activeController = activeTrace.Replay();

            foreach (var pair in activeDeviceMap)
            {
                if (pair.Value != null && pair.Value.added)
                {
                    activeController.WithDeviceMappedFromTo(pair.Key, pair.Value.deviceId);
                }
            }

            activeController.OnFinished(PlayNextSegment);
            if (useTimestamps)
            {
                activeController.PlayAllEventsAccordingToTimestamps();
            }
            else
            {
                activeController.PlayAllFramesOneByOne();
            }
            Repaint();
        }

        private void StepSingleEvent()
        {
            if (segmentFiles.Count == 0) return;
            if (activeController == null)
            {
                ResolveAndMapDevices();
                currentSegmentIndex = 1;
                activeTrace = new InputEventTrace();
                activeTrace.ReadFrom(segmentFiles[0]);
                activeController = activeTrace.Replay();
                foreach (var pair in activeDeviceMap)
                {
                    if (pair.Value != null && pair.Value.added)
                    {
                        activeController.WithDeviceMappedFromTo(pair.Key, pair.Value.deviceId);
                    }
                }
                isPlaying = true;
                isPaused = true;
            }
            activeController.PlayOneEvent();
            Repaint();
        }

        public void StopReplay()
        {
            isPlaying = false;
            isPaused = false;
            pendingStartOnPlayMode = false;
            activeController?.Dispose();
            activeTrace?.Dispose();
            activeController = null;
            activeTrace = null;
            activeDeviceMap.Clear();

            foreach (var device in createdDevices)
            {
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }
            createdDevices.Clear();
            Repaint();
        }

        private void OnGUI()
        {
            HandleDragAndDrop();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("AppMana Unity Input Trace Replay", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Load a downloaded input-trace-*.zip archive, an extracted recording folder, or an individual .inputtrace file to replay recorded session inputs in the Unity Editor.",
                MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            var editedPath = EditorGUILayout.TextField("Recording Source", sourcePath);
            if (editedPath != sourcePath)
            {
                sourcePath = editedPath;
            }
            if (GUILayout.Button("Select .zip / File...", GUILayout.Width(125)))
            {
                var selected = EditorUtility.OpenFilePanel("Select Input Trace (.zip, manifest.json, or .inputtrace)", sourcePath, "zip,inputtrace,json");
                if (!string.IsNullOrEmpty(selected))
                {
                    LoadFromPath(selected);
                }
            }
            if (GUILayout.Button("Select Folder...", GUILayout.Width(105)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Extracted Recording Folder", sourcePath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    LoadFromPath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(sourcePath) && GUILayout.Button("Reload Source"))
            {
                LoadFromPath(sourcePath);
            }

            if (!string.IsNullOrEmpty(integrityError))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(integrityError, MessageType.Error);
            }

            if (segmentFiles.Count > 0)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Recording Metadata", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    if (hasManifest)
                    {
                        EditorGUILayout.LabelField("Recording ID", recordingId);
                        EditorGUILayout.LabelField("Session ID", sessionId);
                        EditorGUILayout.LabelField("Attempt ID", attemptId);
                        EditorGUILayout.LabelField("Unity Version", $"{recordedUnityVersion} (Editor: {Application.unityVersion})");
                        EditorGUILayout.LabelField("Input System", $"{recordedInputSystemVersion} (Editor: {InputSystem.version})");
                        EditorGUILayout.LabelField("Duration", $"{durationSeconds:F2} s");
                        EditorGUILayout.LabelField("Stop Reason", $"{stopReason}{(truncated ? " (truncated)" : "")}");
                        EditorGUILayout.LabelField("SHA-256 Integrity", integrityVerified ? "Verified (all segments match manifest.json)" : "Unverified");
                    }
                    EditorGUILayout.LabelField("Segments", $"{segmentFiles.Count} .inputtrace file(s) ({totalBytes / 1024.0:F1} KiB)");
                    EditorGUILayout.LabelField("Total Events", totalEventCount.ToString("N0"));
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Recorded Input Devices", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    if (recordedDevices.Count == 0)
                    {
                        EditorGUILayout.LabelField("No device headers recorded in trace.");
                    }
                    else
                    {
                        foreach (var dev in recordedDevices)
                        {
                            var match = InputSystem.devices.FirstOrDefault(d => d.added && d.layout == dev.layout);
                            var status = match != null ? $"Maps to '{match.name}' (#{match.deviceId})" : $"Will create virtual '{dev.layout}' device";
                            EditorGUILayout.LabelField($"Device #{dev.deviceId}: {dev.name} ({dev.layout})", status);
                        }
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Playback Options & Controls", EditorStyles.boldLabel);
                autoEnterPlayMode = EditorGUILayout.Toggle("Auto-Enter Play Mode", autoEnterPlayMode);
                useTimestamps = EditorGUILayout.Toggle("Replay by Timestamps (vs Frame Markers)", useTimestamps);
                loopReplay = EditorGUILayout.Toggle("Loop Replay", loopReplay);

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !isPlaying && !pendingStartOnPlayMode;
                if (GUILayout.Button("Play in Editor", GUILayout.Height(28)))
                {
                    StartReplay();
                }
                GUI.enabled = true;

                if (GUILayout.Button("Step 1 Event", GUILayout.Height(28), GUILayout.Width(100)))
                {
                    StepSingleEvent();
                }

                GUI.enabled = isPlaying && activeController != null;
                if (GUILayout.Button(isPaused ? "Resume" : "Pause", GUILayout.Height(28), GUILayout.Width(80)))
                {
                    isPaused = !isPaused;
                    activeController.paused = isPaused;
                }
                GUI.enabled = isPlaying || pendingStartOnPlayMode;
                if (GUILayout.Button("Stop", GUILayout.Height(28), GUILayout.Width(80)))
                {
                    StopReplay();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                if (isPlaying && activeTrace != null && activeController != null)
                {
                    EditorGUILayout.Space(4);
                    var total = Math.Max(1L, activeTrace.eventCount);
                    var pos = activeController.position;
                    var progress = Mathf.Clamp01((float)pos / total);
                    var rect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.ProgressBar(rect, progress,
                        $"Segment {currentSegmentIndex}/{segmentFiles.Count} — Event {pos}/{activeTrace.eventCount}");
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Unity Input Debugger"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Analysis/Input Debugger");
                }
                if (GUILayout.Button("Reveal .inputtrace Files"))
                {
                    EditorUtility.RevealInFinder(segmentFiles[0]);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop()
        {
            var evt = Event.current;
            if (evt == null || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform))
                return;
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                LoadFromPath(DragAndDrop.paths[0]);
            }
            evt.Use();
        }

        private static string ComputeShortPathHash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes, 0, 4).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Zero-dependency ZIP extractor supporting Store (0) and Deflate (8) entries.
        /// Works across all Unity API compatibility levels without requiring System.IO.Compression.FileSystem.
        /// </summary>
        public static void ExtractZipArchive(string zipFilePath, string destinationDirectory)
        {
            var bytes = File.ReadAllBytes(zipFilePath);
            var eocdOffset = -1;
            for (var i = bytes.Length - 22; i >= Math.Max(0, bytes.Length - 65557); i--)
            {
                if (BitConverter.ToUInt32(bytes, i) == 0x06054b50u)
                {
                    eocdOffset = i;
                    break;
                }
            }
            if (eocdOffset < 0)
            {
                throw new InvalidDataException("Invalid .zip file: missing End of Central Directory record.");
            }

            int entryCount = BitConverter.ToUInt16(bytes, eocdOffset + 10);
            int cdOffset = (int)BitConverter.ToUInt32(bytes, eocdOffset + 16);
            var fullDestRoot = Path.GetFullPath(destinationDirectory);

            for (var i = 0; i < entryCount; i++)
            {
                if (BitConverter.ToUInt32(bytes, cdOffset) != 0x02014b50u)
                    throw new InvalidDataException("Invalid .zip Central Directory entry.");

                ushort method = BitConverter.ToUInt16(bytes, cdOffset + 10);
                int compressedSize = (int)BitConverter.ToUInt32(bytes, cdOffset + 20);
                int uncompressedSize = (int)BitConverter.ToUInt32(bytes, cdOffset + 24);
                ushort nameLen = BitConverter.ToUInt16(bytes, cdOffset + 28);
                ushort extraLen = BitConverter.ToUInt16(bytes, cdOffset + 30);
                ushort commentLen = BitConverter.ToUInt16(bytes, cdOffset + 32);
                int localOffset = (int)BitConverter.ToUInt32(bytes, cdOffset + 42);
                var entryName = Encoding.UTF8.GetString(bytes, cdOffset + 46, nameLen);

                cdOffset += 46 + nameLen + extraLen + commentLen;

                if (string.IsNullOrEmpty(entryName) || entryName.EndsWith("/") || entryName.EndsWith("\\"))
                    continue;

                if (BitConverter.ToUInt32(bytes, localOffset) != 0x04034b50u)
                    throw new InvalidDataException("Invalid .zip Local File Header.");

                ushort localNameLen = BitConverter.ToUInt16(bytes, localOffset + 26);
                ushort localExtraLen = BitConverter.ToUInt16(bytes, localOffset + 28);
                int dataStart = localOffset + 30 + localNameLen + localExtraLen;

                var outPath = Path.GetFullPath(Path.Combine(fullDestRoot, entryName));
                if (!outPath.StartsWith(fullDestRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                    !string.Equals(outPath, fullDestRoot, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Refusing to extract unsafe path: {entryName}");
                }

                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

                if (method == 0)
                {
                    using (var fs = File.Create(outPath))
                    {
                        fs.Write(bytes, dataStart, compressedSize);
                    }
                }
                else if (method == 8)
                {
                    using (var compressedStream = new MemoryStream(bytes, dataStart, compressedSize))
                    using (var deflate = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    using (var fs = File.Create(outPath))
                    {
                        deflate.CopyTo(fs);
                        if (fs.Length != uncompressedSize)
                            throw new InvalidDataException($"Decompressed size mismatch for {entryName}");
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported ZIP compression method {method} for {entryName}");
                }
            }
        }
    }
}
