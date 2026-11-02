using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Screen = AppMana.Compatibility.Screen;

namespace AppManaPublic.Tests.Runtime
{
    public class ScreenTests
    {
        [UnitySetUp]
        public IEnumerator WithoutCameras()
        {
            PluginObjects.DestroyAll();
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.Destroy(existing.gameObject);
            }

            yield return null;
        }

        [Test]
        public void SceneWithoutPlayersOrCamerasReportsTheProblemAndUsesTheScreen()
        {
            LogAssert.Expect(LogType.Error, new Regex("Add a RemotePlayableConfiguration"));
            Screen.ScreenCompatibility();

            Assert.That(Screen.camera, Is.Null);
            Assert.That(Screen.width, Is.GreaterThan(0));
            Assert.That(Screen.height, Is.GreaterThan(0));
        }

        [Test]
        public void SceneWithoutPlayersUsesItsCamera()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            LogAssert.Expect(LogType.Error, new Regex("Add a RemotePlayableConfiguration"));
            Screen.ScreenCompatibility();

            Assert.That(Screen.camera, Is.EqualTo(camera));
            Assert.That(Screen.width, Is.EqualTo(camera.pixelWidth));
            Assert.That(Screen.height, Is.EqualTo(camera.pixelHeight));
            Object.DestroyImmediate(camera.gameObject);
        }

        [UnityTest]
        public IEnumerator StreamedSizeComesFromTheCameraTargetAndSurvivesTheCameraBeingDestroyed()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            var target = new RenderTexture(320, 200, 0);
            camera.targetTexture = target;
            Screen.camera = camera;

            Assert.That(Screen.width, Is.EqualTo(320));
            Assert.That(Screen.height, Is.EqualTo(200));

            Object.Destroy(camera.gameObject);
            yield return null;

            Assert.That(Screen.width, Is.GreaterThan(0));
            Assert.That(Screen.height, Is.GreaterThan(0));
            Object.Destroy(target);
            Screen.camera = null;
        }
    }
}
