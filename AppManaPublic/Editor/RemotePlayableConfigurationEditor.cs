using System;
using System.Linq;
using System.Text.Encodings.Web;
using AppManaPublic.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
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

                EditorGUILayout.PropertyField(m_AudioListener,
                    new GUIContent
                    {
                        text = "Audio Listener",
                        tooltip = "Set this to the audio listener for this player, or leave null to disable audio"
                    });
                EditorGUILayout.PropertyField(m_Actions,
                    new GUIContent
                    {
                        text = "Actions Asset",
                        tooltip =
                            "Leave null. In multiplayer games: duplicate the Input Actions Asset provided by the plugin into your own Assets/ directory, and set it here."
                    });
                EditorGUILayout.PropertyField(m_CanvasScalers,
                    new GUIContent
                    {
                        text = "Canvas Scalers",
                        tooltip = "Set this to canvas scalers for the player's canvas, used to adjust display DPI"
                    });

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
                    var targetOfflineUrlParameters = m_Target.offlineUrlParameters;
                    var example = targetOfflineUrlParameters
                        .Select((kv, i) => (kv, i))
                        .Aggregate("?", (s, kvi) =>
                        {
                            var amp = kvi.i > 0 ? "&" : "";
                            return s +
                                   $"{amp}{UrlEncoder.Default.Encode(kvi.kv.Key)}={UrlEncoder.Default.Encode(kvi.kv.Value)}";
                        });
                    if (targetOfflineUrlParameters.Count > 0)
                    {
                        EditorGUILayout.LabelField("Encoded:");
                        EditorGUILayout.LabelField(example);
                    }
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