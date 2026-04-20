using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AppMana.Editor
{
  public static class ShaderVariantCollector
  {
    private const string k_AssetPath = "Assets/Settings/CollectedShaderVariants.shadervariants";

    [MenuItem("Tools/Save Tracked Shader Variants")]
    private static void SaveTrackedVariants()
    {
      var directory = System.IO.Path.GetDirectoryName(k_AssetPath);
      if (!AssetDatabase.IsValidFolder(directory))
      {
        System.IO.Directory.CreateDirectory(directory);
        AssetDatabase.Refresh();
      }

      // ShaderUtil.SaveCurrentShaderVariantCollection was removed in Unity 6.
      // Use reflection to call the internal method if available, otherwise fall back
      // to the Graphics Settings "Save to asset..." approach.
      var shaderUtilType = typeof(ShaderUtil);
      var saveMethod = shaderUtilType.GetMethod("SaveCurrentShaderVariantCollection",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
        null, new[] { typeof(string) }, null);

      if (saveMethod != null)
      {
        saveMethod.Invoke(null, new object[] { k_AssetPath });
      }
      else
      {
        // Fallback: get the currently tracked collection via GraphicsSettings internal API
        var getMethod = shaderUtilType.GetMethod("GetCurrentShaderVariantCollectionShaderCount",
          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (getMethod != null)
        {
          int count = (int)getMethod.Invoke(null, null);
          Debug.Log($"[ShaderVariantCollector] Tracked shader count: {count}");
        }

        // Try the internal GraphicsSettings method
        var gsType = typeof(GraphicsSettings);
        var gsInternal = gsType.Assembly.GetType("UnityEditor.ShaderUtil");
        if (gsInternal != null)
        {
          saveMethod = gsInternal.GetMethod("SaveCurrentShaderVariantCollection",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
          if (saveMethod != null)
          {
            saveMethod.Invoke(null, new object[] { k_AssetPath });
          }
        }

        if (saveMethod == null)
        {
          // Last resort: guide user to do it manually
          Debug.LogError(
            "[ShaderVariantCollector] Could not find SaveCurrentShaderVariantCollection API. " +
            "Please save manually: Project Settings > Graphics > Shader Loading > Save to asset...");
          SettingsService.OpenProjectSettings("Project/Graphics");
          return;
        }
      }

      AssetDatabase.Refresh();

      var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(k_AssetPath);
      if (collection != null)
      {
        Debug.Log($"[ShaderVariantCollector] Saved {collection.variantCount} tracked shader variants to {k_AssetPath}");
        AddToPreloadedShaders(collection);
      }
      else
      {
        Debug.LogError($"[ShaderVariantCollector] Failed to save shader variant collection to {k_AssetPath}");
      }
    }

    [MenuItem("Tools/Clear Tracked Shader Variants")]
    private static void ClearTracked()
    {
      var shaderUtilType = typeof(ShaderUtil);
      var clearMethod = shaderUtilType.GetMethod("ClearCurrentShaderVariantCollection",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

      if (clearMethod != null)
      {
        clearMethod.Invoke(null, null);
        Debug.Log("[ShaderVariantCollector] Cleared tracked shader variant list. Play the scene to start collecting new variants.");
      }
      else
      {
        Debug.LogError(
          "[ShaderVariantCollector] Could not find ClearCurrentShaderVariantCollection API. " +
          "Please clear manually: Project Settings > Graphics > Shader Loading");
        SettingsService.OpenProjectSettings("Project/Graphics");
      }
    }

    private static void AddToPreloadedShaders(ShaderVariantCollection collection)
    {
      var graphicsSettings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
      if (graphicsSettings == null) return;

      var so = new SerializedObject(graphicsSettings);
      var preloadedProp = so.FindProperty("m_PreloadedShaders");

      for (int i = 0; i < preloadedProp.arraySize; i++)
      {
        if (preloadedProp.GetArrayElementAtIndex(i).objectReferenceValue == collection)
        {
          Debug.Log("[ShaderVariantCollector] Collection already in preloaded shaders list.");
          return;
        }
      }

      bool replaced = false;
      for (int i = 0; i < preloadedProp.arraySize; i++)
      {
        if (preloadedProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
        {
          preloadedProp.GetArrayElementAtIndex(i).objectReferenceValue = collection;
          replaced = true;
          break;
        }
      }

      if (!replaced)
      {
        preloadedProp.InsertArrayElementAtIndex(preloadedProp.arraySize);
        preloadedProp.GetArrayElementAtIndex(preloadedProp.arraySize - 1).objectReferenceValue = collection;
      }

      so.ApplyModifiedProperties();
      Debug.Log("[ShaderVariantCollector] Added collection to Graphics Settings > Preloaded Shaders.");
    }
  }
}
