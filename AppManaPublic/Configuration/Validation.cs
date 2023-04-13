using System.Linq;
using AppMana.UI.TMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace AppManaPublic.Configuration
{
    /// <summary>
    /// Checks your project for configuration errors.
    /// </summary>
    internal class Validation
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void ValidateEditor()
        {
            if (!InputSystemV144EditorPlayerSettingHelpers.newSystemBackendsEnabled ||
                InputSystemV144EditorPlayerSettingHelpers.oldSystemBackendsEnabled)
            {
                Debug.LogError("You must enable Input System as the exclusive Input handling backend under " +
                               "Player Settings > Active Input Handling. This will correctly log errors if your game" +
                               $" executes {nameof(Input)} methods, like {nameof(Input.GetAxis)}, including third party" +
                               $" libraries, at runtime.");
            }

            var enabledScenes = EditorBuildSettings.scenes.Count(scene => scene.enabled);
            if (enabledScenes == 0)
            {
                Debug.LogError("You must enable a scene for building in your Build Settings window.");
            }

            if (enabledScenes > 1)
            {
                Debug.LogWarning("You are building multiple scenes. You can only load additional scenes " +
                                 $"additively. Using {nameof(SceneManager)}.{nameof(SceneManager.LoadScene)} is not " +
                                 $"supported because AppMana cannot guarantee you do not destroy the streaming camera.");
            }
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Validate()
        {
            // perform checks for common mistakes and tries to resolve them automatically

            // check cameras
            var remotePlayableConfigurations = Object.FindObjectsOfType<RemotePlayableConfiguration>(true);
            if (remotePlayableConfigurations.Length == 0)
            {
                Debug.LogError($"Add a {nameof(RemotePlayableConfiguration)} to your scene to enable streaming.");
            }

            var cameras = Object.FindObjectsOfType<Camera>();
            var unassociatedCameras = cameras.Where(camera => camera.enabled
                                                              && camera.GetComponent<RenderNonStreamingCamera>() == null
                                                              && camera.targetTexture == null
                                                              && remotePlayableConfigurations.All(rpc =>
                                                                  rpc.camera != camera)).ToArray();
            if (unassociatedCameras.Length == 1)
            {
                Debug.LogWarning(
                    $"Connect this camera to a {nameof(RemotePlayableConfiguration)}, add a {nameof(RenderNonStreamingCamera)} component to ensure it is rendered, or disable it.",
                    unassociatedCameras[0]);
            }

            if (unassociatedCameras.Length > 1)
            {
                Debug.LogError($"AppMana does not support multiple, active cameras that (1) are not associated with " +
                               $"a {nameof(RemotePlayableConfiguration)} and (2) are not rendering to a {nameof(RenderTexture)}. " +
                               $"You must use the SRP-appropriate approach for camera stacking instead, such as " +
                               $"compositing or render layers.", cameras[0]);
            }

            // check for input fields
            var inputFields = Object.FindObjectsOfType<InputField>(true);
            var tmpInputFields = Object.FindObjectsOfType<TMP_InputField>(true);
            if (inputFields.Length > 0 || tmpInputFields.Length > 0)
            {
                Debug.LogError(
                    $"AppMana does not support Unity's native {nameof(InputField)} and {nameof(TMP_InputField)} " +
                    $"components. They incorrectly still use IMGUI for text input when using Input System. We rewrote " +
                    $"the component to use Input System alone. Please use {nameof(TMP_InputSystemInputField)} by" +
                    $" switching your Inspector to Debug mode, selecting your {nameof(TMP_InputField)} object, then " +
                    $"dragging and dropping our {nameof(TMP_InputSystemInputField)} into the script slot on the " +
                    $"component. {inputFields.Length} {nameof(InputField)}(s) and {tmpInputFields} " +
                    $"{nameof(TMP_InputField)}(s) were found that need to be updated.");
            }


            var inputSystemTMPInputFieldModules = Object.FindObjectsOfType<InputSystemTMPInputFieldModule>(true);
            if (Object.FindObjectsOfType<TMP_InputSystemInputField>(true).Length > 0 &&
                inputSystemTMPInputFieldModules.Length !=
                remotePlayableConfigurations.Length)
            {
                Debug.LogError($"You must attach a {nameof(InputSystemTMPInputFieldModule)} to a game object for" +
                               $" every {nameof(RemotePlayableConfiguration)} (player in your game). The " +
                               $"{nameof(TMP_InputSystemInputField)} searches its parent hierarchy to find its " +
                               $"corresponding {nameof(InputSystemTMPInputFieldModule)} in order to function, so all " +
                               $"{nameof(InputSystemTMPInputFieldModule)} components should be children of your " +
                               $"{nameof(RemotePlayableConfiguration)}(s).");
            }


            // check for screen space overlay canvases
            foreach (var canvas in Object.FindObjectsOfType<Canvas>(true)
                         .Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay))
            {
                var guessCamera = canvas.GetComponentInParent<Camera>()
                                  ?? (remotePlayableConfigurations.Length > 0
                                      ? remotePlayableConfigurations[0].camera
                                      : null)
                                  ?? canvas.transform.root.GetComponentInChildren<Camera>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = guessCamera;
                Debug.LogWarning(
                    $"A Screen Space Overlay canvas was found and is not supported by AppMana by the name of " +
                    $"{canvas.gameObject.name}. Switching to {nameof(RenderMode.ScreenSpaceCamera)} and using camera " +
                    $"{guessCamera?.gameObject.name ?? "(not found)"}",
                    canvas);
            }
        }
    }
}