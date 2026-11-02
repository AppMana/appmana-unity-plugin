using System.Collections;
using AppMana.Multiplayer;
using AppManaPublic.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AppManaPublic.Tests.Runtime
{
    public class StreamedInputsTests
    {
        [SetUp]
        public void WithoutPluginObjects()
        {
            PluginObjects.DestroyAll();
        }

        private static RemotePlayableConfiguration InactivePlayer(string name, Camera camera)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            var player = root.AddComponent<RemotePlayableConfiguration>();
            player.camera = camera;
            return player;
        }

        [UnityTest]
        public IEnumerator PlayersWithoutCamerasAreSkippedWhileOthersAreFixed()
        {
            var withCamera = InactivePlayer("Player 1", null);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(withCamera.transform);
            camera.gameObject.AddComponent<PhysicsRaycaster>();
            withCamera.camera = camera;
            var withoutCamera = InactivePlayer("Player 2", null);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Set the Actions field"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Set the Actions field"));
            withCamera.gameObject.SetActive(true);
            withoutCamera.gameObject.SetActive(true);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("PhysicsRaycaster on Camera was replaced"));
            StreamedInputs.FixRaycasters();
            yield return null;

            Assert.That(camera.GetComponent<PerUserPhysicsRaycaster>(), Is.Not.Null);
            Object.Destroy(withCamera.gameObject);
            Object.Destroy(withoutCamera.gameObject);
        }

        [UnityTest]
        public IEnumerator SinglePlayerWithADestroyedCameraStillFixesCanvasRaycasters()
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            var player = InactivePlayer("Player", camera);
            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Object.Destroy(camera.gameObject);
            yield return null;

            StreamedInputs.FixRaycasters();
            yield return null;

            Assert.That(canvas.GetComponent<PerUserGraphicRaycaster>(), Is.Not.Null);
            Object.Destroy(canvas.gameObject);
            Object.Destroy(player.gameObject);
        }

        [UnityTest]
        public IEnumerator DestroyedStreamedInputsIsForgottenAndClosingItsLobbyWithoutAHostDoesNotThrow()
        {
            AppMana.ComponentModel.AppManaHostBase.instance = null;
            var streamedInputs = new GameObject("(StreamedInputs)").AddComponent<StreamedInputs>();
            Assert.That(StreamedInputs.instance, Is.EqualTo(streamedInputs));

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("Called CloseLobby"));
            streamedInputs.CloseLobby();

            Object.Destroy(streamedInputs.gameObject);
            yield return null;
            Assert.That(StreamedInputs.instance, Is.Null);
        }
    }
}
