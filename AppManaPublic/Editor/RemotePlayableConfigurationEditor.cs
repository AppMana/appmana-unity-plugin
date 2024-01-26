using System;
using System.Linq;
using AppMana.ComponentModel;
using AppMana.InteractionToolkit;
using AppManaPublic.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using PlayerPrefs = AppMana.Compatibility.PlayerPrefs;

namespace AppManaPublic.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RemotePlayableConfiguration))]
    internal class RemotePlayableConfigurationEditor : UnityEditor.Editor
    {
        private static readonly bool m_HasPrivatePlugin;
        private RemotePlayableConfiguration m_Target;
        private SerializedProperty m_Camera;
        private SerializedProperty m_AudioListener;
        private SerializedProperty m_Actions;
        private SerializedProperty m_CanvasScalers;
        private SerializedProperty m_OnPlayerConnected;
        private SerializedProperty m_OnPlayerDisconnected;
        private SerializedProperty m_StreamInEditMode;
        private SerializedProperty m_EnablePlayerPrefs;
        private SerializedProperty m_EnableUrlParameters;
        private SerializedProperty m_EnableAugmentedReality;
        private SerializedProperty m_EnableAllInputActions;
        private SerializedProperty m_OfflineUrlParameters;
        private SerializedProperty m_RotationCoefficient;
        private SerializedProperty m_PositionCoefficient;

        static RemotePlayableConfigurationEditor()
        {
            m_HasPrivatePlugin = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "AppMana");
        }

        private void OnEnable()
        {
            m_Target = (RemotePlayableConfiguration)target;
            m_Camera = serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_Camera));
            m_AudioListener = serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_AudioListener));
            m_Actions = serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_Actions));
            m_CanvasScalers = serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_CanvasScalers));
            m_OnPlayerConnected =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_OnPlayerConnected));
            m_OnPlayerDisconnected =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_OnPlayerDisconnected));
            m_StreamInEditMode = serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_StreamInEditMode));
            m_EnablePlayerPrefs =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_EnablePlayerPrefs));
            m_EnableUrlParameters =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_EnableUrlParameters));
            m_EnableAugmentedReality =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_EnableAugmentedReality));
            m_OfflineUrlParameters =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_OfflineUrlParameters));
            m_RotationCoefficient =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_RotationCoefficient));
            m_PositionCoefficient =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_PositionCoefficient));
            m_EnableAllInputActions =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_EnableAllInputActions));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            try
            {
                var remotePlayableConfigurations =
                    UnityUtilities.FindObjectsByType<RemotePlayableConfiguration>();
                var myIndex = Array.IndexOf(remotePlayableConfigurations, m_Target);

                EditorGUILayout.PropertyField(m_Camera,
                    new GUIContent { text = "Camera", tooltip = "Set this to the camera to stream for this player" });
                if (m_Camera.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Set the camera. Otherwise, you will see no picture when streaming.",
                        MessageType.Error);
                    if (EditorGUILayout.LinkButton("Set to main camera"))
                    {
                        m_Camera.objectReferenceValue = Cameras.guessedMainCamera;
                    }
                }

                if (remotePlayableConfigurations.Length > 1 && remotePlayableConfigurations[0].camera != null)
                {
                    if (remotePlayableConfigurations.Select(configuration => configuration.camera?.targetDisplay ?? 0)
                            .Distinct().Count() != remotePlayableConfigurations.Length)
                    {
                        EditorGUILayout.HelpBox(
                            $"To visualize a multiplayer game correctly in the editor, each camera needs a distinct {nameof(Camera.targetDisplay)}. Then, create another Game view by right clicking on your existing one, then navigating to Add Tab, then choosing Game View. Finally, set the Target Display of the game view using the Display dropdown on the tab.",
                            MessageType.Error);
                        if (EditorGUILayout.LinkButton("Set distinct target displays."))
                        {
                            for (var i = 0; i < remotePlayableConfigurations.Length; i++)
                            {
                                var configuration = remotePlayableConfigurations[i];
                                configuration.camera.targetDisplay = i;
                                EditorUtility.SetDirty(configuration.camera);
                            }
                        }
                    }
                }

                EditorGUILayout.PropertyField(m_AudioListener,
                    new GUIContent
                    {
                        text = "Audio Listener",
                        tooltip = "Set this to the audio listener for this player, or leave null to disable audio"
                    });
                var audioListeners = UnityUtilities.FindObjectsByType<AudioListener>();
                if (m_AudioListener.objectReferenceValue == null &&
                    EditorGUILayout.LinkButton("Set to pre-existing audio listener or create one"))
                {
                    if (audioListeners.Length == 0)
                    {
                        var audioListener = new GameObject("Audio Listener");
                        audioListeners = new[] { Undo.AddComponent<AudioListener>(audioListener) };
                    }

                    m_AudioListener.objectReferenceValue = audioListeners[0];
                }

                if (audioListeners.Length > 1)
                {
                    if (audioListeners.Count(al => al.isActiveAndEnabled) == 1 &&
                        audioListeners.First(al => al.isActiveAndEnabled) != m_AudioListener.objectReferenceValue)
                    {
                        EditorGUILayout.HelpBox(
                            $"While only one {nameof(AudioListener)} is active, it isn't referenced on this {nameof(RemotePlayableConfiguration)}. You will not hear sound.",
                            MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"To hear sound, make sure exactly one {nameof(AudioListener)} is active and that it is the one referenced on the {nameof(RemotePlayableConfiguration)}.",
                            MessageType.Error);
                    }
                }

                EditorGUILayout.PropertyField(m_Actions,
                    new GUIContent
                    {
                        text = "Actions Asset",
                        tooltip =
                            "The Input Actions corresponding to this player."
                    });
                var standaloneInputModules = UnityUtilities.FindObjectsByType<StandaloneInputModule>();
                if (m_Actions.objectReferenceValue != null && standaloneInputModules.Length > 0)
                {
                    EditorGUILayout.HelpBox(
                        "You have EventSystem objects with StandaloneInputModule, which isn't compatible.",
                        MessageType.Error);
                    if (EditorGUILayout.LinkButton("Replace with Input System Event System modules."))
                    {
                        foreach (var standaloneInputModule in standaloneInputModules)
                        {
                            var go = standaloneInputModule.gameObject;
                            Undo.DestroyObjectImmediate(standaloneInputModule);
                            var newModule = Undo.AddComponent<InputSystemUIInputModule>(go);
                            newModule.actionsAsset = (InputActionAsset)m_Actions.objectReferenceValue;
                        }
                    }
                }

                if (m_Actions.objectReferenceValue == null &&
                    EditorGUILayout.LinkButton("Create and assign input actions."))
                {
                    EditorGUILayout.HelpBox(
                        "Assign an Input Actions Asset. This enables AppMana streamed inputs to work.",
                        MessageType.Error);

                    const string duplicateAndAssignDistinctInputActions = "Distinct Input Actions";
                    const string sourcePath =
                        "Packages/com.appmana.unity.public/AppManaPublic/Input Actions.inputactions";
                    var targetPath = $"Assets/AppMana Input Actions.inputactions";
                    if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
                    {
                        throw new ArgumentException("could not copy");
                    }

                    AssetDatabase.Refresh();

                    var newAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
                    Undo.RegisterCreatedObjectUndo(newAsset, duplicateAndAssignDistinctInputActions);
                    Undo.RegisterCompleteObjectUndo(target, duplicateAndAssignDistinctInputActions);
                    m_Actions.objectReferenceValue = newAsset;

                    if (remotePlayableConfigurations.Length == 1)
                    {
                        var inputSystemEventModule = UnityUtilities.FindAnyObjectByType<InputSystemUIInputModule>();
                        if (inputSystemEventModule != null)
                        {
                            inputSystemEventModule.actionsAsset = (InputActionAsset)newAsset;
                            EditorUtility.SetDirty(inputSystemEventModule);
                        }
                    }
                }

                EditorGUILayout.PropertyField(m_CanvasScalers,
                    new GUIContent
                    {
                        text = "Canvas Scalers",
                        tooltip = "Set this to canvas scalers for the player's canvas, used to adjust display DPI"
                    });
                var relevantCanvasScalers = remotePlayableConfigurations.Length > 1
                    ? m_Target.GetComponentsInChildren<CanvasScaler>(true)
                    : UnityUtilities.FindObjectsByType<CanvasScaler>();
                if (m_CanvasScalers.arraySize != relevantCanvasScalers.Length &&
                    EditorGUILayout.LinkButton("Assign missing canvas scalers"))
                {
                    m_CanvasScalers.ClearArray();
                    for (var i = 0; i < relevantCanvasScalers.Length; i++)
                    {
                        m_CanvasScalers.InsertArrayElementAtIndex(i);
                        m_CanvasScalers.GetArrayElementAtIndex(i).objectReferenceValue = relevantCanvasScalers[0];
                    }
                }

                EditorGUILayout.PropertyField(m_OnPlayerConnected,
                    new GUIContent
                    {
                        text = "On Player Connected", tooltip = "Called when this player connects to the experience"
                    });
                EditorGUILayout.HelpBox(
                    "Use the On Player Connected event instead of Start to start your game. Start is called long before this event, when no user is connected. Perform asset loading in Start to ensure your game loads quickly. Don't put code that loads effects and models in On Player Connected; use Start. Don't start your game in Start, wait until a player connects in On Player Connected.",
                    MessageType.Info);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_OnPlayerDisconnected,
                    new GUIContent
                    {
                        text = "On Player Disconnected",
                        tooltip = "Called when this player disconnects from the experience"
                    });

                EditorGUILayout.PropertyField(m_EnablePlayerPrefs,
                    new GUIContent
                    {
                        text = "Enable PlayerPrefs",
                        tooltip =
                            $"Enables loading the PlayerPrefs from the user's local storage in their browser. Access the player's PlayerPrefs using {nameof(RemotePlayableConfiguration)}.{nameof(RemotePlayableConfiguration.playerPrefs)}. You can also use {nameof(AppMana)}.{nameof(AppMana.Compatibility)}.{nameof(PlayerPrefs)} for compatibility with existing usage of PlayerPrefs."
                    });

                EditorGUILayout.PropertyField(m_EnableAugmentedReality,
                    new GUIContent
                    {
                        text = "Enable Augmented Reality",
                        tooltip =
                            $"todo"
                    });

                EditorGUILayout.PropertyField(m_EnableUrlParameters,
                    new GUIContent
                    {
                        text = "Enable URL Params",
                        tooltip =
                            $"Loads the URL parameters from the visitor's browser. Access the player's URL parameters using {nameof(RemotePlayableConfiguration)}.{nameof(RemotePlayableConfiguration.urlParameters)}."
                    });

                if (m_EnableUrlParameters.boolValue)
                {
                    EditorGUILayout.PropertyField(m_OfflineUrlParameters,
                        new GUIContent
                        {
                            text = "Offline / simulated URL parameters",
                            tooltip =
                                "Set these key value pairs to the URL parameters when running offline / not in a streaming session, such as in the editor or when the player is built standalone."
                        });
                }

                EditorGUI.BeginDisabledGroup(remotePlayableConfigurations.Length > 1);
                EditorGUILayout.PropertyField(m_EnableAllInputActions,
                    new GUIContent
                    {
                        text = "Enable All Input Action References",
                        tooltip =
                            $"Ensures that all actions associated with input action references in your scene are enabled when your game starts, wherever they are."
                    });
                EditorGUI.EndDisabledGroup();
                if (remotePlayableConfigurations.Length > 1)
                {
                    var inputActionReferencesInScene = UnityUtilities.FindObjectsByType<InputActionReference>();
                    if (inputActionReferencesInScene.Length > 0 && !Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox(
                            $"You have ordinary {nameof(InputActionReference)} objects in your scene, but you are making a multiplayer game. Use {nameof(MultiplayerInputActionReference)}, and add the actions to the {nameof(InputActionAsset)} associated with this component ({m_Actions.serializedObject?.targetObject?.name ?? "none set"}) instead to ensure each user's devices are correctly associated with their corresponding actions.",
                            MessageType.Error);
                        foreach (var inputActionReference in inputActionReferencesInScene)
                        {
                            var content = new GUIContent(inputActionReference.name);
                            var rect = GUILayoutUtility.GetRect(content, EditorStyles.linkLabel);

                            if (GUI.Button(rect, content, EditorStyles.linkLabel))
                            {
                                Selection.activeObject = inputActionReference;
                            }
                        }
                    }
                }


                if (m_EnableAugmentedReality.boolValue)
                {
                    EditorGUILayout.HelpBox(new GUIContent
                    {
                        text =
                            $"Experimental augmented reality features are enabled. When using AR Foundation, attach a TrackedPoseDriver to the objects you want to follow the device. When AR Foundation is not enabled in your project, the camera's local position and local orientation will be set to the camera pose instead."
                    });
                }

                if (m_EnablePlayerPrefs.boolValue || m_EnableUrlParameters.boolValue)
                {
                    EditorGUILayout.HelpBox(new GUIContent
                    {
                        text =
                            $"{nameof(RemotePlayableConfiguration.playerPrefs)} and {nameof(RemotePlayableConfiguration.urlParameters)} can only be accessed after a player has been connected. You are notified of a connection in {nameof(RemotePlayableConfiguration)}.{nameof(RemotePlayableConfiguration.onPlayerConnected)}, either by dragging and dropping a method into the event slot in the editor or by calling {nameof(RemotePlayableConfiguration)}.{nameof(RemotePlayableConfiguration.onPlayerConnected)}.{nameof(UnityEvent.AddListener)}."
                    });
                }


                if (m_HasPrivatePlugin)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_StreamInEditMode,
                        new GUIContent { text = "Stream in edit mode" });
                    if (m_Target.m_EnableUrlParameters)
                    {
                        EditorGUILayout.HelpBox(new GUIContent
                        {
                            text =
                                $"URL Parameters: {m_Target.urlParameters?.As<JObject>()?.ToString(Formatting.Indented) ?? "(null)"}"
                        });
                    }

                    EditorGUILayout.PropertyField(m_PositionCoefficient,
                        new GUIContent { text = "Position Coefficient" });
                    EditorGUILayout.PropertyField(m_RotationCoefficient,
                        new GUIContent { text = "Rotiation Coefficient" });
                }
                else
                {
                    m_StreamInEditMode.boolValue = false;
                }
            }
            finally
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}