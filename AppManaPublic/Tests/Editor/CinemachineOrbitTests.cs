#if (CINEMACHINE_3 || APPMANA_CINEMACHINE_3) && !CINEMACHINE_2 && !APPMANA_CINEMACHINE_2
#define APPMANA_TEST_CM3
using Unity.Cinemachine;
#elif CINEMACHINE || CINEMACHINE_2 || APPMANA_CINEMACHINE_2
#define APPMANA_TEST_CM2
using Cinemachine;
#endif
using AppMana.InteractionToolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AppManaPublic.Editor.Tests
{
    public class CinemachineOrbitTests
    {
#if (APPMANA_TEST_CM2 || APPMANA_TEST_CM3) && ENABLE_INPUT_SYSTEM
        [Test]
        public void PressableInputProvider_ReturnsZeroWhenDisabled()
        {
            var go = new GameObject("OrbitCamera");
            try
            {
                var provider = go.AddComponent<CinemachinePressableInputProvider>();
                provider.values = new[] { 12.5f, -7.25f, 1.5f };

                provider.enabled = true;
                Assert.That(provider.GetAxisValue(0), Is.EqualTo(12.5f));
                Assert.That(provider.GetAxisValue(1), Is.EqualTo(-7.25f));
                Assert.That(provider.GetAxisValue(2), Is.EqualTo(1.5f));

                provider.enabled = false;
                Assert.That(provider.GetAxisValue(0), Is.EqualTo(0f));
                Assert.That(provider.GetAxisValue(1), Is.EqualTo(0f));
                Assert.That(provider.GetAxisValue(2), Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PressableInputProvider_GatesXYAxesOnEnableWhenPressedAndAutoEnablesActions()
        {
            var go = new GameObject("OrbitCamera");
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                var map = asset.AddActionMap("UI");
                var clickAction = map.AddAction("Click", InputActionType.PassThrough, "<Pointer>/press");
                var deltaAction = map.AddAction("Delta", InputActionType.PassThrough, "<Pointer>/delta");
                var scrollAction = map.AddAction("ScrollWheel", InputActionType.PassThrough, "<Pointer>/scroll");

                var clickRef = InputActionReference.Create(clickAction);
                var deltaRef = InputActionReference.Create(deltaAction);
                var scrollRef = InputActionReference.Create(scrollAction);

                var provider = go.AddComponent<CinemachinePressableInputProvider>();
                provider.autoEnableInputs = true;
                provider.enableWhenPressed = clickRef;
                provider.xyAxis = deltaRef;
                provider.zAxis = scrollRef;
                provider.ReinitializeInputActions();

                Assert.That(clickAction.enabled, Is.True);
                Assert.That(deltaAction.enabled, Is.True);
                Assert.That(scrollAction.enabled, Is.True);

                provider.values = new[] { 10f, -5f, 2f };
                Assert.That(provider.GetAxisValue(0), Is.EqualTo(0f));
                Assert.That(provider.GetAxisValue(1), Is.EqualTo(0f));
                Assert.That(provider.GetAxisValue(2), Is.EqualTo(2f));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EnableWhenDragged_TogglesProviderOnDragLifecycle()
        {
            var root = new GameObject("CanvasRoot");
            var camGo = new GameObject("MainCamera");
            var providerGo = new GameObject("OrbitCamera");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.cullingMask = 1 << 0;
                var physicsRaycaster = camGo.AddComponent<PhysicsRaycaster>();
                physicsRaycaster.eventMask = 1 << 6;

                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                var graphicRaycaster = root.AddComponent<GraphicRaycaster>();
                graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.ThreeD;
                graphicRaycaster.blockingMask = ~0;

                var bgGo = new GameObject("Background");
                bgGo.layer = 5;
                bgGo.transform.SetParent(root.transform, false);
                var canvasRenderer = bgGo.AddComponent<CanvasRenderer>();
                canvasRenderer.cullTransparentMesh = true;
                var image = bgGo.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                var dragReceiver = bgGo.AddComponent<CinemachineEnableWhenDragged>();

                var provider = providerGo.AddComponent<CinemachinePressableInputProvider>();
                dragReceiver.inputProvider = provider;

                dragReceiver.SendMessage("Start");

                Assert.That(provider.enabled, Is.False);
                Assert.That(canvasRenderer.cullTransparentMesh, Is.False);
                Assert.That((int)graphicRaycaster.blockingMask, Is.EqualTo(1 << 6));
                Assert.That(cam.cullingMask & (1 << 5), Is.Not.EqualTo(0));

                dragReceiver.OnBeginDrag(null);
                Assert.That(provider.enabled, Is.True);

                dragReceiver.OnDrag(null);
                Assert.That(provider.enabled, Is.True);

                dragReceiver.OnEndDrag(null);
                Assert.That(provider.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(providerGo);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(root);
            }
        }

#if !CINEMACHINE_NO_CM2_SUPPORT
#pragma warning disable CS0618
        [Test]
        public void PressableInputProvider_BindsToCinemachinePOV()
        {
            var go = new GameObject("VirtualCamera");
            try
            {
                var vcam = go.AddComponent<CinemachineVirtualCamera>();
                var pov = vcam.AddCinemachineComponent<CinemachinePOV>();
                var provider = go.AddComponent<CinemachinePressableInputProvider>();
                provider.ReinitializeInputActions();

                Assert.That(pov.m_HorizontalAxis.HasInputProvider, Is.True);
                Assert.That(pov.m_VerticalAxis.HasInputProvider, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
#pragma warning restore CS0618
#endif

#if APPMANA_TEST_CM3
        [Test]
        public void PressableInputProvider_DrivesCinemachine3PanTiltAxes()
        {
            var go = new GameObject("Cinemachine3Camera");
            try
            {
                go.AddComponent<CinemachineCamera>();
                var panTilt = go.AddComponent<CinemachinePanTilt>();
                var provider = go.AddComponent<CinemachinePressableInputProvider>();
                provider.ReinitializeInputActions();
                provider.values = new[] { 10f, -8f, 0f };

                provider.SendMessage("Update");

                Assert.That(panTilt.PanAxis.Value, Is.Not.EqualTo(0f));
                Assert.That(panTilt.TiltAxis.Value, Is.Not.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
#endif
#endif
    }
}
