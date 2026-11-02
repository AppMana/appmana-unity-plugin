using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AppManaPublic.Tests.Runtime
{
    /// <summary>
    /// Scene assets that play mode tests load in <see cref="LoadSceneMode.Single"/> mode, the way a game switches
    /// levels: an empty scene, and a level scene with its own main camera and event system.
    /// </summary>
    public sealed class EmptyTestScene : IPrebuildSetup, IPostBuildCleanup
    {
        internal const string folder = "Assets/AppManaPublicTestScenes";
        internal const string emptyPath = folder + "/Empty.unity";
        internal const string levelPath = folder + "/Level.unity";
        internal const string levelCameraName = "Level Camera";

        public void Setup()
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(folder);
            AssetDatabase.CreateFolder("Assets", "AppManaPublicTestScenes");

            {
                var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(empty, emptyPath);
                EditorSceneManager.CloseScene(empty, true);
            }

            {
                var level = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var camera = new GameObject(levelCameraName).AddComponent<Camera>();
                camera.tag = "MainCamera";
                SceneManager.MoveGameObjectToScene(camera.gameObject, level);
                var eventSystem = new GameObject("Level EventSystem").AddComponent<EventSystem>();
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                SceneManager.MoveGameObjectToScene(eventSystem.gameObject, level);
                EditorSceneManager.SaveScene(level, levelPath);
                EditorSceneManager.CloseScene(level, true);
            }
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(folder);
#endif
        }

        internal static AsyncOperation LoadEmpty() => Load(emptyPath);

        internal static AsyncOperation LoadLevel() => Load(levelPath);

        private static AsyncOperation Load(string path)
        {
#if UNITY_EDITOR
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            return null;
#endif
        }
    }
}
