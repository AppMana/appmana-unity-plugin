using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using AppMana.Compatibility;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    [PrebuildSetup(typeof(EmptyTestScene)), PostBuildCleanup(typeof(EmptyTestScene))]
    public class MultipleScenesTests
    {
        private static RemotePlayableConfiguration InactiveConfiguration(Camera camera)
        {
            // The player's own Awake applies no mitigations, so each test applies exactly the ones under test.
            var root = new GameObject("Player");
            root.SetActive(false);
            var configuration = root.AddComponent<RemotePlayableConfiguration>();
            configuration.m_EnableMultipleSceneMitigations = false;
            root.SetActive(true);
            configuration.camera = camera;
            return configuration;
        }

        private static Camera RootMainCamera(string name)
        {
            var camera = new GameObject(name).AddComponent<Camera>();
            camera.tag = "MainCamera";
            return camera;
        }

        [UnitySetUp]
        public IEnumerator StartFromAnEmptyScene()
        {
            PluginObjects.DestroyAll();
            yield return EmptyTestScene.LoadEmpty();
        }

        [UnityTearDown]
        public IEnumerator UnloadPreservedObjects()
        {
            foreach (var preserved in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(gameObject => gameObject.scene.name == "DontDestroyOnLoad" && gameObject.transform.parent == null)
                         .Where(gameObject => gameObject.GetComponent<RemotePlayableConfiguration>() != null ||
                                              gameObject.GetComponent<Camera>() != null ||
                                              gameObject.GetComponent<EventSystem>() != null ||
                                              gameObject.GetComponent<Canvas>() != null))
            {
                Object.Destroy(preserved);
            }

            PluginObjects.DestroyAll();

            yield return null;
        }

        [UnityTest]
        public IEnumerator SingleModeLoadThatUnloadsTheStreamedCameraDoesNotThrow()
        {
            var level = new GameObject("Level");
            var camera = RootMainCamera("Main Camera");
            camera.transform.SetParent(level.transform);
            var configuration = InactiveConfiguration(camera);

            MultipleScenes.MultipleScenesMitigateDestructionOfRemotePlayableConfiguration(configuration);
            LogAssert.Expect(LogType.Warning, new Regex("DontDestroyOnLoad only works for root GameObjects"));
            MultipleScenes.MultipleScenesMitigateDestructionOfCamera(camera);
            MultipleScenes.MultipleScenesMitigateMultipleMainCameras(configuration);

            LogAssert.Expect(LogType.Error, new Regex("streamed camera .* was destroyed"));
            yield return EmptyTestScene.LoadLevel();
            // reported once the unload finishes
            yield return null;
            yield return null;

            Assert.That(configuration != null, "the configuration was preserved across the load");
            Assert.That(camera == null, "the nested camera was unloaded with its scene");
        }

        [UnityTest]
        public IEnumerator PreservedCameraFollowsTheNextScenesMainCamera()
        {
            var camera = RootMainCamera("Streamed Camera");
            var configuration = InactiveConfiguration(camera);
            MultipleScenes.MultipleScenesMitigateDestructionOfRemotePlayableConfiguration(configuration);
            MultipleScenes.MultipleScenesMitigateDestructionOfCamera(camera);
            MultipleScenes.MultipleScenesMitigateMultipleMainCameras(configuration);

            yield return EmptyTestScene.LoadLevel();
            yield return null;

            var levelCamera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(candidate => candidate.name == EmptyTestScene.levelCameraName);
            Assert.That(camera != null, "the streamed camera was preserved");
            Assert.That(levelCamera.gameObject.activeSelf, Is.False, "the level's main camera was disabled");
            var constraint = camera.GetComponent<ParentConstraint>();
            Assert.That(constraint.constraintActive, Is.True);
            Assert.That(constraint.sourceCount, Is.EqualTo(1));
            Assert.That(constraint.GetSource(0).sourceTransform, Is.EqualTo(levelCamera.transform));
        }

        [UnityTest]
        public IEnumerator InputModuleMitigationWithoutAnInputModuleDoesNotThrow()
        {
            var configuration = InactiveConfiguration(RootMainCamera("Streamed Camera"));

            MultipleScenes.MultipleScenesMitigateMultipleInputModules(configuration);
            yield return EmptyTestScene.LoadLevel();
            yield return null;

            var levelModule = Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(candidate => candidate.name == "Level EventSystem");
            Assert.That(levelModule.enabled, Is.True, "with nothing to preserve, the level keeps its own input module");
        }

        [UnityTest]
        public IEnumerator InputModuleMitigationDisablesTheNextScenesEventSystem()
        {
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            var configuration = InactiveConfiguration(RootMainCamera("Streamed Camera"));
            MultipleScenes.MultipleScenesMitigateDestructionOfRemotePlayableConfiguration(configuration);
            MultipleScenes.MultipleScenesMitigateMultipleInputModules(configuration);

            yield return EmptyTestScene.LoadLevel();
            yield return null;

            Assert.That(module != null && module.enabled, "the first scene's input module was preserved");
            var levelModule = Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(candidate => candidate.name == "Level EventSystem");
            Assert.That(levelModule.enabled, Is.False);
            Assert.That(levelModule.GetComponent<EventSystem>().enabled, Is.False);
        }

        [Test]
        public void PreservingMissingObjectsDoesNothing()
        {
            MultipleScenes.MultipleScenesMitigateDestructionOfCamera(null);
            MultipleScenes.MultipleScenesMitigateDestructionOfRemotePlayableConfiguration(null);
        }

        [UnityTest]
        public IEnumerator ScreenSpaceOverlayMitigationWithoutACameraDoesNotThrow()
        {
            var canvas = new GameObject("Overlay").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.DontDestroyOnLoad(canvas.gameObject);
            MultipleScenes.MultipleScenesMitigateScreenSpaceOverlays(null);

            yield return EmptyTestScene.LoadEmpty();
            yield return null;

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), "without a camera, overlays stay as they are");
        }

        [UnityTest]
        public IEnumerator ScreenSpaceOverlayCanvasesRenderThroughThePreservedCamera()
        {
            var camera = RootMainCamera("Streamed Camera");
            Object.DontDestroyOnLoad(camera.gameObject);
            MultipleScenes.MultipleScenesMitigateScreenSpaceOverlays(camera);
            var canvas = new GameObject("Overlay").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.DontDestroyOnLoad(canvas.gameObject);

            yield return EmptyTestScene.LoadEmpty();
            yield return null;

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(canvas.worldCamera, Is.EqualTo(camera));
            Object.Destroy(canvas.gameObject);
        }
    }
}
