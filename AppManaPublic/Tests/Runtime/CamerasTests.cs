using System.Collections;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    public class CamerasTests
    {
        [SetUp]
        public void WithoutPluginObjects()
        {
            PluginObjects.DestroyAll();
        }

        [UnityTest]
        public IEnumerator FirstAliveSkipsDestroyedAndMissingCameras()
        {
            var destroyed = new GameObject("Destroyed").AddComponent<Camera>();
            var alive = new GameObject("Alive").AddComponent<Camera>();
            Object.Destroy(destroyed.gameObject);
            yield return null;

            Assert.That(Cameras.FirstAlive(destroyed, null, alive), Is.EqualTo(alive));
            Assert.That(Cameras.FirstAlive(destroyed, null), Is.Null);
            Assert.That(Cameras.FirstAlive(), Is.Null);
            Object.Destroy(alive.gameObject);
        }

        [UnityTest]
        public IEnumerator GuessedMainCameraPrefersTheMainCameraThenAnyCameraThenAnInactiveCamera()
        {
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.Destroy(existing.gameObject);
            }

            yield return null;
            Assert.That(Cameras.guessedMainCamera, Is.Null);

            var inactive = new GameObject("Inactive").AddComponent<Camera>();
            inactive.gameObject.SetActive(false);
            Assert.That(Cameras.guessedMainCamera, Is.EqualTo(inactive));

            var any = new GameObject("Any").AddComponent<Camera>();
            Assert.That(Cameras.guessedMainCamera, Is.EqualTo(any));

            var main = new GameObject("Main").AddComponent<Camera>();
            main.tag = "MainCamera";
            Assert.That(Cameras.guessedMainCamera, Is.EqualTo(main));

            Object.Destroy(inactive.gameObject);
            Object.Destroy(any.gameObject);
            Object.Destroy(main.gameObject);
        }
    }
}
