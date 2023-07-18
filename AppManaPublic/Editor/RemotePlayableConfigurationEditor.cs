using System;
using System.Linq;
using AppMana.ComponentModel;
using AppManaPublic.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
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
        private SerializedProperty m_OfflineUrlParameters;

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
            m_OfflineUrlParameters =
                serializedObject.FindProperty(nameof(RemotePlayableConfiguration.m_OfflineUrlParameters));
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
                        audioListeners = new[] { audioListener.AddComponent<AudioListener>() };
                        EditorUtility.SetDirty(audioListener);
                        Undo.RegisterCreatedObjectUndo(audioListener, "Undo Create Audio Listener");
                    }

                    m_AudioListener.objectReferenceValue = audioListeners[0];
                }

                if (audioListeners.Length > 1)
                {
                    EditorGUILayout.HelpBox(
                        "You cannot use more than one AudioListener due to limitations in Unity.",
                        MessageType.Error);
                    if (EditorGUILayout.LinkButton("Delete the extra AudioListener"))
                    {
                        foreach (var audioListener in audioListeners[new Range(1, Index.End)])
                        {
                            if (m_AudioListener.objectReferenceValue == audioListener)
                            {
                                m_AudioListener.objectReferenceValue = audioListeners[0];
                            }

                            Undo.DestroyObjectImmediate(audioListener);
                        }
                    }
                }

                EditorGUILayout.PropertyField(m_Actions,
                    new GUIContent
                    {
                        text = "Actions Asset",
                        tooltip =
                            "The Input Actions corresponding to this player."
                    });


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