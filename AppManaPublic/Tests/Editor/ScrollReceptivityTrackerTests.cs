using System;
using AppMana.ComponentModel;
using AppMana.InteractionToolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AppManaPublic.Editor.Tests
{
    public class ScrollReceptivityTrackerTests
    {
        private sealed class CustomScrollConsumer : MonoBehaviour, IHasInputActionReferences
        {
            public InputActionReference zoomAction;

            public IHasInputActionReferences.InputActionReferenceProperty[] inputActionReferenceProperties => new[]
            {
                new IHasInputActionReferences.InputActionReferenceProperty(
                    () => zoomAction,
                    value => zoomAction = value)
            };
        }

        [Test]
        public void IgnoresInputSystemUIInputModuleDefaultScrollWheelAction()
        {
            var go = new GameObject("EventSystemWithUIModule");
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            using var tracker = new ScrollReceptivityTracker();
            try
            {
                var map = asset.AddActionMap("UI");
                var scrollAction = map.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll");
                scrollAction.Enable();

                go.AddComponent<EventSystem>();
                var uiModule = go.AddComponent<InputSystemUIInputModule>();
                uiModule.scrollWheel = InputActionReference.Create(scrollAction);

                tracker.InvalidateCache();
                Assert.That(tracker.HasActiveScrollAction(), Is.False);
                Assert.That(tracker.TryUpdateState(out var capturesScroll), Is.True);
                Assert.That(capturesScroll, Is.False);
                Assert.That(tracker.StateHash, Is.EqualTo(0));

                // Subsequent check with unchanged state returns false (no redundant network message)
                Assert.That(tracker.TryUpdateState(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(go);
            }
        }

#if (CINEMACHINE || CINEMACHINE_2 || APPMANA_CINEMACHINE_2 || CINEMACHINE_3 || APPMANA_CINEMACHINE_3) && ENABLE_INPUT_SYSTEM
        [Test]
        public void DetectsCinemachinePressableInputProviderOnlyWhenActiveWithZAxisAndNonZeroMultiplier()
        {
            var go = new GameObject("OrbitCamera");
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            using var tracker = new ScrollReceptivityTracker();
            try
            {
                var map = asset.AddActionMap("Camera");
                var scrollAction = map.AddAction("Zoom", InputActionType.PassThrough, "<Mouse>/scroll");
                scrollAction.Enable();
                var scrollRef = InputActionReference.Create(scrollAction);

                var provider = go.AddComponent<CinemachinePressableInputProvider>();
                provider.scrollWheelMultiplier = 0.1f;
                provider.zAxis = null;

                tracker.InvalidateCache();
                Assert.That(tracker.HasActiveScrollAction(), Is.False);

                provider.zAxis = scrollRef;
                Assert.That(tracker.HasActiveScrollAction(), Is.True);
                Assert.That(tracker.TryUpdateState(out var captures), Is.True);
                Assert.That(captures, Is.True);
                Assert.That(tracker.StateHash, Is.EqualTo(1));

                provider.scrollWheelMultiplier = 0f;
                Assert.That(tracker.HasActiveScrollAction(), Is.False);

                provider.scrollWheelMultiplier = 0.1f;
                provider.enabled = false;
                Assert.That(tracker.HasActiveScrollAction(), Is.False);
                Assert.That(tracker.TryUpdateState(out captures), Is.True);
                Assert.That(captures, Is.False);
                Assert.That(tracker.StateHash, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(go);
            }
        }
#endif

        [Test]
        public void DetectsCustomMonoBehaviourWithScrollInputActionReference()
        {
            var go = new GameObject("ZoomController");
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            using var tracker = new ScrollReceptivityTracker();
            try
            {
                var map = asset.AddActionMap("Gameplay");
                var zoomAction = map.AddAction("CameraZoom", InputActionType.Value, "<Pointer>/scroll");
                var zoomRef = InputActionReference.Create(zoomAction);

                var consumer = go.AddComponent<CustomScrollConsumer>();
                consumer.zoomAction = zoomRef;

                tracker.InvalidateCache();
                // Action not yet enabled
                Assert.That(tracker.HasActiveScrollAction(), Is.False);

                zoomAction.Enable();
                Assert.That(tracker.HasActiveScrollAction(), Is.True);

                zoomAction.Disable();
                Assert.That(tracker.HasActiveScrollAction(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(asset);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EvaluatesUguiScrollRectOverflowAndNonScrollEventTriggerPassthrough()
        {
            var root = new GameObject("Canvas");
            using var tracker = new ScrollReceptivityTracker();
            try
            {
                var viewportGo = new GameObject("Viewport", typeof(RectTransform));
                viewportGo.transform.SetParent(root.transform, false);
                var viewportRect = viewportGo.GetComponent<RectTransform>();
                viewportRect.sizeDelta = new Vector2(200f, 200f);

                var contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentRect = contentGo.GetComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(200f, 100f);

                var scrollRect = viewportGo.AddComponent<ScrollRect>();
                scrollRect.viewport = viewportRect;
                scrollRect.content = contentRect;
                scrollRect.vertical = true;
                scrollRect.horizontal = false;

                var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Image));
                itemGo.transform.SetParent(contentGo.transform, false);

                var clickTriggerGo = new GameObject("ClickTriggerChild", typeof(RectTransform));
                clickTriggerGo.transform.SetParent(contentGo.transform, false);
                clickTriggerGo.AddComponent<EventTrigger>();

                // Non-overflowing content (height 100 <= viewport 200): should not capture scroll
                Assert.That(ScrollReceptivityTracker.CanScrollRectScroll(scrollRect), Is.False);
                Assert.That(tracker.IsGameObjectScrollableUgui(itemGo), Is.False);
                Assert.That(tracker.IsGameObjectScrollableUgui(clickTriggerGo), Is.False);

                // Overflowing content (height 600 > viewport 200): captures scroll on both item and nested non-scroll EventTrigger
                contentRect.sizeDelta = new Vector2(200f, 600f);
                Assert.That(ScrollReceptivityTracker.CanScrollRectScroll(scrollRect), Is.True);
                Assert.That(tracker.IsGameObjectScrollableUgui(itemGo), Is.True);
                Assert.That(tracker.IsGameObjectScrollableUgui(clickTriggerGo), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
