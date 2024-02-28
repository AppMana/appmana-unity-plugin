using System.Collections.Generic;
using System.Linq;
using AppMana.ComponentModel;
using AppMana.Multiplayer;
using AppManaPublic.Configuration;
using UniRx;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

namespace AppMana.Compatibility
{
    /// <summary>
    /// Contains methods to mitigate issues when dealing with multiple scenes in Unity, such as handling screen space overlays, raycasters, input modules, main cameras, and preventing destruction of certain objects.
    /// </summary>
    /// <remarks>This addresses multiple scene usage in the plugin. Invoke these methods separately, or implement your own mitigations. These are invoked by default in <see cref="RemotePlayableConfiguration"/>.</remarks>
    public class MultipleScenes
    {
        /// <summary>
        /// Converts Canvas objects from ScreenSpaceOverlay to ScreenSpaceCamera to ensure they render correctly in a multi-scene setup.
        /// </summary>
        /// <param name="camera">The Camera to be used by the Canvas objects.</param>
        /// <remarks>This method subscribes to the scene loaded observable to automatically adjust Canvases when a new scene is loaded.</remarks>
        public static void MultipleScenesMitigateScreenSpaceOverlays(Camera camera)
        {
            UnityUtilities.OnSceneLoadedAsObservable()
                .Subscribe(_ =>
                {
                    var canvases = UnityUtilities.FindObjectsByType<Canvas>(true)
                        .Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                                         (canvas.renderMode == RenderMode.ScreenSpaceCamera && !canvas.worldCamera));
                    foreach (var canvas in canvases)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = camera;
                        canvas.planeDistance = 1.1f * camera.nearClipPlane;
                    }
                })
                .AddTo(camera);
        }

        /// <summary>
        /// Fixes issues with raycasters in a multi-scene setup to ensure UI elements interact correctly.
        /// </summary>
        /// <remarks>This method subscribes to the scene loaded observable to automatically fix raycasters when a new scene is loaded. Raycasters should be "per user" by using the <see cref="PerUserGraphicRaycaster"/>, <see cref="PerUserPhysicsRaycaster"/> and <see cref="PerUserPhysics2DRaycaster"/> raycaster components.</remarks>
        public static void MultipleScenesMitigateRaycasters()
        {
            UnityUtilities.OnSceneLoadedAsObservable()
                .Subscribe(_ => { StreamedInputs.FixRaycasters(); });
        }

        /// <summary>
        /// Disables other input modules to prevent conflicts in a multi-scene setup, preserving only the input module from the original scene.
        /// </summary>
        /// <param name="remotePlayableConfiguration">The configuration object containing the active scene's input module.</param>
        /// <remarks>This method subscribes to the scene loaded observable to automatically adjust input modules when a new scene is loaded. New scenes will often duplicate this functionality incorrectly.</remarks>
        public static void MultipleScenesMitigateMultipleInputModules(
            RemotePlayableConfiguration remotePlayableConfiguration)
        {
            var thisSceneInputModule = UnityUtilities.FindAnyObjectByType<BaseInputModule>();
            Object.DontDestroyOnLoad(thisSceneInputModule);
            UnityUtilities.OnSceneLoadedAsObservable()
                .Subscribe(_ =>
                {
                    var otherInputModules = UnityUtilities.FindObjectsByType<BaseInputModule>(true)
                        .Where(otherInputModule => otherInputModule != thisSceneInputModule);
                    foreach (var otherInputModule in otherInputModules)
                    {
                        var correspondingEventSystemGuess =
                            otherInputModule.GetComponentInChildren<EventSystem>();
                        // todo: do these need to be destroyed instead?
                        correspondingEventSystemGuess.enabled = false;
                        otherInputModule.enabled = false;
                    }
                })
                .AddTo(remotePlayableConfiguration);
        }

        /// <summary>
        /// Manages multiple main cameras in a multi-scene setup by adjusting their parent constraints to match a specified configuration.
        /// </summary>
        /// <param name="remotePlayableConfiguration">The configuration object containing the reference camera.</param>
        /// <remarks>This method subscribes to the scene loaded observable to automatically manage main cameras when a new scene is loaded.</remarks>
        public static void MultipleScenesMitigateMultipleMainCameras(
            RemotePlayableConfiguration remotePlayableConfiguration)
        {
            UnityUtilities.OnSceneLoadedAsObservable()
                .Subscribe(_ =>
                {
                    var parentConstraint = remotePlayableConfiguration.camera.GetComponent<ParentConstraint>();
                    if (parentConstraint == null)
                    {
                        parentConstraint =
                            remotePlayableConfiguration.camera.gameObject.AddComponent<ParentConstraint>();
                    }

                    var otherMainCameras = UnityUtilities.FindObjectsByType<Camera>(true)
                        .Where(camera => camera.CompareTag("MainCamera"))
                        .Where(otherCamera => remotePlayableConfiguration.camera != otherCamera)
                        .ToArray();

                    parentConstraint.constraintActive = false;
                    parentConstraint.SetSources(new List<ConstraintSource>());

                    if (otherMainCameras.Length > 1)
                    {
                        Debug.LogError(
                            "Mitigations cannot be applied to this scene: there are too many other main cameras being added.");
                        return;
                    }

                    // at this point, there is only 1 other main camera.
                    foreach (var otherMainCamera in otherMainCameras)
                    {
                        var source = new ConstraintSource
                        {
                            sourceTransform = otherMainCamera.transform,
                            weight = 1
                        };
                        parentConstraint.AddSource(source);
                        parentConstraint.translationAxis = Axis.X | Axis.Y | Axis.Z;
                        parentConstraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
                        parentConstraint.translationOffsets = new[] { Vector3.zero };
                        parentConstraint.rotationOffsets = new[] { Vector3.zero };
                        otherMainCamera.gameObject.SetActive(false);
                    }

                    parentConstraint.constraintActive = true;
                })
                .AddTo(remotePlayableConfiguration);
        }

        /// <summary>
        /// Prevents the specified camera from being destroyed across scene loads.
        /// </summary>
        /// <param name="target">The Camera object to preserve.</param>
        public static void MultipleScenesMitigateDestructionOfCamera(Camera target)
        {
            Object.DontDestroyOnLoad(target);
        }

        /// <summary>
        /// Prevents the RemotePlayableConfiguration object from being destroyed across scene loads.
        /// </summary>
        /// <param name="remotePlayableConfiguration">The RemotePlayableConfiguration object to preserve.</param>
        public static void MultipleScenesMitigateDestructionOfRemotePlayableConfiguration(
            RemotePlayableConfiguration remotePlayableConfiguration)
        {
            Object.DontDestroyOnLoad(remotePlayableConfiguration);
        }
    }
}