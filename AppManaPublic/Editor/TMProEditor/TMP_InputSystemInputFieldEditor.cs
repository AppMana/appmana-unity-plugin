using AppMana.UI.TMPro;
using UnityEditor;
#if TMP_EDITOR
using TMPro.EditorUtilities;
#endif

namespace AppManaPublic.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TMP_InputSystemInputField), true)]
    public class TMP_InputSystemInputFieldEditor :
#if TMP_EDITOR
        TMP_InputFieldEditor
#else
        UnityEditor.Editor
#endif
    {
    }
}