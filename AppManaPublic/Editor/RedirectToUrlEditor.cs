using AppMana.InteractionToolkit;
using UnityEditor;
using UnityEngine;

namespace AppManaPublic.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RedirectToUrl))]
    class RedirectToUrlEditor : UnityEditor.Editor
    {
        private RedirectToUrl m_RedirectToUrl;
        private SerializedProperty m_BrowserHref;
        private SerializedProperty m_AppendCurrentSearchParams;
        private SerializedProperty m_AppendAnchor;

        public void OnEnable()
        {
            m_RedirectToUrl = (RedirectToUrl)target;
            m_BrowserHref = serializedObject.FindProperty(nameof(m_BrowserHref));
            m_AppendCurrentSearchParams = serializedObject.FindProperty(nameof(m_AppendCurrentSearchParams));
            m_AppendAnchor = serializedObject.FindProperty(nameof(m_AppendAnchor));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Call Execute() on this script to redirect to a URL.", MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_BrowserHref, new GUIContent("Destination"));
            EditorGUILayout.HelpBox(
                "The destination should be a relative or absolute URL. The format is the same as the format of the href attribute for HTML anchor tags. Some examples are (without the quotes) \"https://example.com/destination/path\", \"/destination/path\" and \"destination/path\".",
                MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_AppendCurrentSearchParams,
                new GUIContent("Include parameters"));
            EditorGUILayout.HelpBox(
                "When checked, the browser will grab the pre-existing search parameters on the page, like \"?fbclickid=12345\", and append them to the redirect URL. This enables forwarding of \"utm_campaign\" and \"fbclickid\" parameters and should be checked by default.",
                MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_AppendAnchor, new GUIContent("Append custom anchor"));
            EditorGUILayout.HelpBox(
                "When set to non-empty content, adds a \"#anchor\" to the end of your URL. This can be used to navigate to a specific place on the destination page",
                MessageType.None);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Computed JavaScript");
            EditorGUILayout.LabelField(m_RedirectToUrl.script);
            EditorGUILayout.HelpBox(
                "Use the script above to verify that your redirect works. You can copy and paste it into the JavaScript console in your Chrome DevTools.",
                MessageType.None);
            serializedObject.ApplyModifiedProperties();
        }
    }
}