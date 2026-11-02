using System.Collections;
using AppMana.ComponentModel;
using AppMana.InteractionToolkit;
using AppMana.Multiplayer;
using AppManaPublic.Configuration;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AppManaPublic.Tests.Runtime
{
    public class HostAndInteractionTests
    {
        [SetUp]
        public void WithoutPluginObjects()
        {
            PluginObjects.DestroyAll();
        }

        private sealed class RecordingHost : AppManaHostBase
        {
            internal int closedLobbies;
            internal int augmentedRealityEnabled;

            internal override void CloseLobby() => closedLobbies++;

            internal override UniTask<JToken> EvalInPage(string javascript, RemotePlayableConfiguration player = null,
                bool ignoreResult = false) => UniTask.FromResult<JToken>(null);

            internal override void EnableAugmentedReality() => augmentedRealityEnabled++;
        }

        [UnityTearDown]
        public IEnumerator DestroyPlayersAndPlugins()
        {
            foreach (var behaviour in Object.FindObjectsByType<RemotePlayableConfiguration>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.Destroy(behaviour.gameObject);
            }

            PluginObjects.DestroyAll();

            yield return null;
        }

        private static RecordingHost StartHost()
        {
            var host = new GameObject("Host").AddComponent<RecordingHost>();
            AppManaHostBase.instance = host;
            return host;
        }

        [UnityTest]
        public IEnumerator DestroyedHostIsForgotten()
        {
            var host = StartHost();
            Object.Destroy(host.gameObject);
            yield return null;

            Assert.That(AppManaHostBase.instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator ClosingTheLobbyWithoutStreamedInputsReachesTheHost()
        {
            StreamedInputs.ResetForPlayMode();
            var host = StartHost();

#pragma warning disable CS0612
            StreamedMultiplayer.instance.CloseLobby();
#pragma warning restore CS0612
            yield return null;

            Assert.That(host.closedLobbies, Is.EqualTo(1));
            Object.Destroy(host.gameObject);
        }

        [Test]
        public void ClosingTheLobbyWithoutAHostDoesNotThrow()
        {
            AppManaHostBase.instance = null;
#pragma warning disable CS0612
            StreamedMultiplayer.instance.CloseLobby();
#pragma warning restore CS0612
        }

        [UnityTest]
        public IEnumerator AugmentedRealityStartsWhenTheHostAppearsLater()
        {
            var root = new GameObject("Player");
            root.SetActive(false);
            var configuration = root.AddComponent<RemotePlayableConfiguration>();
            configuration.camera = new GameObject("Camera").AddComponent<Camera>();
            configuration.m_EnableAugmentedReality = true;
            root.SetActive(true);
            yield return null;
            yield return null;

            var host = StartHost();
            yield return null;
            yield return null;

            Assert.That(host.augmentedRealityEnabled, Is.EqualTo(1));
            Object.Destroy(host.gameObject);
            Object.Destroy(configuration.camera.gameObject);
        }

        [UnityTest]
        public IEnumerator ConstrainedPositionFallsBackToTheMainCameraWhenGivenADestroyedCamera()
        {
            var main = new GameObject("Main").AddComponent<Camera>();
            main.tag = "MainCamera";
            main.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
            var destroyed = new GameObject("Destroyed").AddComponent<Camera>();
            Object.Destroy(destroyed.gameObject);
            yield return null;

            // built in code, so serialization never allocated its raycast buffers
            var constraint = RaycastConstraint.xzPlane;
            var center = new Vector2(main.pixelWidth / 2f, main.pixelHeight / 2f);
            var position = constraint.GetWorldPositionConstrained(center, camera: destroyed);

            Assert.That(position.HasValue);
            Assert.That(position.Value.y, Is.EqualTo(0).Within(1e-3));
            Assert.That(position.Value.x, Is.EqualTo(0).Within(1e-2));
            Object.Destroy(main.gameObject);
        }
    }
}
