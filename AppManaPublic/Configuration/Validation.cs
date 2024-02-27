using System.Linq;
using AppMana.ComponentModel;
using AppMana.UI.TMPro;
#if TMP
using TMPro;
#endif
using UnityEngine;
#if UNITY_INPUTSYSTEM
using AppMana.InteractionToolkit;
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;

// ReSharper disable Unity.NoNullPropagation
#endif

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Checks your project for configuration errors.
    /// </summary>
    internal class Validation
    {
        private const string validationPrefix = "Validation Error: ";
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void ValidateEditor()
        {
            if (!InputSystemV144EditorPlayerSettingHelpers.newSystemBackendsEnabled ||
                InputSystemV144EditorPlayerSettingHelpers.oldSystemBackendsEnabled)
            {
                Debug.LogError(
                    $"{validationPrefix}You must enable Input System as the exclusive Input handling backend under " +
                    "Project Settings > Player > Active Input Handling (also found as Player Settings > Active Input Handling). This will correctly log errors if your game" +
                    $" executes {nameof(Input)} methods, like {nameof(Input.GetAxis)}, including third party" +
                    $" libraries, at runtime.");
            }

            var enabledScenes = EditorBuildSettings.scenes.Count(scene => scene.enabled);
            if (enabledScenes == 0)
            {
                Debug.LogError(
                    $"{validationPrefix}You must enable a scene for building in your Build Settings window.");
            }
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Validate()
        {
            // perform checks for common mistakes and tries to resolve them automatically

            // check cameras
            var remotePlayableConfigurations = UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>(true);
            if (remotePlayableConfigurations.Length == 0)
            {
                if (Application.isEditor)
                {
                    Debug.LogError($"{validationPrefix}Add a {nameof(RemotePlayableConfiguration)} to your scene.");
                }
                else
                {
                    Debug.LogError(
                        $"{validationPrefix}Add a {nameof(RemotePlayableConfiguration)} to your scene. One will be created for you.");
                }
            }

            var cameras = UnityUtilities.FindObjectsByType<Camera>();
            var unassociatedCameras = cameras.Where(camera => camera.enabled
                                                              && camera.GetComponent<RenderNonStreamingCamera>() == null
                                                              && camera.targetTexture == null
                                                              && remotePlayableConfigurations.All(rpc =>
                                                                  rpc.camera != camera)).ToArray();
            if (unassociatedCameras.Length == 1 && remotePlayableConfigurations.Length != 0)
            {
                Debug.LogWarning(
                    $"{validationPrefix}Connect {unassociatedCameras[0].gameObject.name} to a {nameof(RemotePlayableConfiguration)}, add a {nameof(RenderNonStreamingCamera)} component to ensure it is rendered, or disable it.",
                    unassociatedCameras[0]);
            }

            if (unassociatedCameras.Length > 1)
            {
                Debug.LogError(
                    $"{validationPrefix}AppMana does not support multiple, active cameras that (1) are not associated with " +
                    $"a {nameof(RemotePlayableConfiguration)} and (2) are not rendering to a {nameof(RenderTexture)}. " +
                    $"You must use the SRP-appropriate approach for camera stacking instead, such as " +
                    $"compositing or render layers; or, if the camera is being used for an effect, add a {nameof(RenderNonStreamingCamera)} to it.",
                    cameras[0]);
            }

            foreach (var remotePlayableConfiguration in remotePlayableConfigurations)
            {
                if (remotePlayableConfiguration.camera == null)
                {
                    Debug.LogFormat(Application.isEditor ? LogType.Error : LogType.Warning, LogOption.NoStacktrace,
                        remotePlayableConfiguration,
                        $"{validationPrefix}Set the {nameof(remotePlayableConfiguration.camera)} field on the {nameof(RemotePlayableConfiguration)} component attached to {remotePlayableConfiguration.gameObject.name}");
                }
            }

#if TMP
            // check for input fields
            var inputFields = UnityUtilities.FindObjectsByType<InputField>(true);
            var tmpInputFields = UnityUtilities.FindObjectsByType<TMP_InputField>(true);
            if (inputFields.Length > 0 || tmpInputFields.Length > 0)
            {
                var monoBehaviours = inputFields.Concat<MonoBehaviour>(tmpInputFields).ToArray();
                var gameObjectNames = string.Join(", ", monoBehaviours.Select(comp => comp.gameObject.name));
                Debug.LogError(
                    $"{validationPrefix}AppMana does not support Unity's native {nameof(InputField)} and {nameof(TMP_InputField)} " +
                    $"components. They incorrectly still use IMGUI for text input when using Input System. We rewrote " +
                    $"the component to use Input System alone. Please use {nameof(TMP_InputSystemInputField)} by" +
                    $" switching your Inspector to Debug mode, selecting your {nameof(TMP_InputField)} object, then " +
                    $"dragging and dropping our {nameof(TMP_InputSystemInputField)} into the script slot on the " +
                    $"component. {inputFields.Length} {nameof(InputField)}(s) and {tmpInputFields.Length} " +
                    $"{nameof(TMP_InputField)}(s) were found that need to be updated.\n{gameObjectNames}",
                    monoBehaviours[0]);
            }


            var inputSystemTMPInputFieldModules =
                UnityUtilities.FindObjectsByType<InputSystemTMPInputFieldModule>(true);
            if (UnityUtilities.FindObjectsByType<TMP_InputSystemInputField>(true).Length > 0 &&
                inputSystemTMPInputFieldModules.Length !=
                remotePlayableConfigurations.Length)
            {
                if (remotePlayableConfigurations.Length == 0)
                {
                    Debug.LogError(
                        $"{validationPrefix}You must attach a {nameof(InputSystemTMPInputFieldModule)} to a game" +
                        $" object that is the parent of all the {nameof(TMP_InputSystemInputField)} you use.");
                }
                else
                {
                    Debug.LogError(
                        $"{validationPrefix}You must attach a {nameof(InputSystemTMPInputFieldModule)} to a game object for" +
                        $" every {nameof(RemotePlayableConfiguration)} (player in your game). The " +
                        $"{nameof(TMP_InputSystemInputField)} searches its parent hierarchy to find its " +
                        $"corresponding {nameof(InputSystemTMPInputFieldModule)} in order to function, so all " +
                        $"{nameof(InputSystemTMPInputFieldModule)} components should be children of your " +
                        $"{nameof(RemotePlayableConfiguration)}(s).");
                }
            }
#endif


            // check for screen space overlay canvases
            foreach (var canvas in UnityUtilities.FindObjectsByType<Canvas>(true)
                         .Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                var guessCamera = canvas.GetComponentInParent<Camera>()
                                  ?? (remotePlayableConfigurations.Length > 0
                                      ? remotePlayableConfigurations[0].camera
                                      : null)
                                  ?? canvas.transform.root.GetComponentInChildren<Camera>()
                                  ?? Cameras.guessedMainCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = guessCamera;
                canvas.planeDistance = guessCamera.nearClipPlane + 0.001f;
                Debug.LogWarning(
                    $"{validationPrefix}A Screen Space Overlay canvas was found and is not supported by AppMana by the name of " +
                    $"{canvas.gameObject.name}. Switching to {nameof(RenderMode)}.{nameof(RenderMode.ScreenSpaceCamera)} and using camera " +
                    $"{guessCamera?.gameObject.name ?? "(not found)"}. Remove this warning by changing the Render Mode of the camera to Screen Space - Camera and choosing an appropriate Distance.",
                    canvas);
            }
            
            if (remotePlayableConfigurations.Length == 0)
            {
                PluginBase.EnsurePlugins();
            }
        }
    }
}