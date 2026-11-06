using System;
using System.Collections.Generic;
using System.Reflection;
using AppMana.InteractionToolkit;
#if TMP
using AppMana.UI.TMPro;
#endif
using AppManaPublic.Configuration;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Tracks whether a streamed player currently has an active non-UI scroll <see cref="InputAction"/>
    /// or has its pointer positioned over an active, scrollable UGUI <see cref="IScrollHandler"/>.
    /// Maintains a single integer state hash (-1 uninitialized, 0 false, 1 true) with zero per-frame allocations.
    /// </summary>
    public sealed class ScrollReceptivityTracker : IDisposable
    {
        private static readonly Dictionary<Type, FieldInfo[]> s_ActionFieldsByType = new();
        private static readonly FieldInfo s_MultiplayerActionRefField =
            typeof(MultiplayerInputActionReference).GetField("m_InputActionReference",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly RemotePlayableConfiguration m_Player;
        private readonly List<CinemachinePressableInputProvider> m_CinemachineProviders = new(4);
        private readonly List<(MonoBehaviour owner, Func<InputActionReference> getter)> m_ActionRefGetters = new(8);
        private readonly List<(MonoBehaviour owner, FieldInfo field)> m_ReflectedActionFields = new(8);
        private readonly List<RaycastResult> m_RaycastResults = new(16);
        private readonly List<IScrollHandler> m_ScrollHandlers = new(4);

        private InputDevice m_PointerDevice;
        private ExtendedPointerEventData m_PointerEventData;
        private EventSystem m_CachedEventSystem;
        private Vector2 m_LastPointerPosition;
        private bool m_HasPointerPosition;
        private bool m_ActionCacheDirty = true;
        private int m_LastStateHash = -1;
        private bool m_Disposed;

        public ScrollReceptivityTracker() : this(null, null)
        {
        }

        public ScrollReceptivityTracker(RemotePlayableConfiguration player, InputDevice pointerDevice = null)
        {
            m_Player = player;
            m_PointerDevice = pointerDevice;
            SceneManager.sceneLoaded += OnSceneChanged;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public int StateHash => m_LastStateHash;

        public bool CapturesScroll => m_LastStateHash == 1;

        public InputDevice PointerDevice
        {
            get => m_PointerDevice;
            set => m_PointerDevice = value;
        }

        public void InvalidateCache()
        {
            m_ActionCacheDirty = true;
        }

        public void SetPointerPosition(Vector2 position)
        {
            m_LastPointerPosition = position;
            m_HasPointerPosition = true;
        }

        public bool TryUpdateState(Vector2 pointerPosition, out bool capturesScroll)
        {
            SetPointerPosition(pointerPosition);
            return TryUpdateState(out capturesScroll);
        }

        public bool TryUpdateState(out bool capturesScroll)
        {
            capturesScroll = Evaluate();
            var nextHash = capturesScroll ? 1 : 0;
            if (nextHash == m_LastStateHash)
            {
                return false;
            }

            m_LastStateHash = nextHash;
            return true;
        }

        public bool Evaluate()
        {
            if (HasActiveScrollAction())
            {
                return true;
            }

            return m_HasPointerPosition && IsPointerOverScrollableUgui(m_LastPointerPosition);
        }

        public bool HasActiveScrollAction()
        {
            if (m_ActionCacheDirty)
            {
                RebuildActionCache();
            }

            for (var i = 0; i < m_CinemachineProviders.Count; i++)
            {
                var provider = m_CinemachineProviders[i];
                if (provider == null || !provider.isActiveAndEnabled)
                {
                    continue;
                }

                if (Mathf.Approximately(provider.scrollWheelMultiplier, 0f))
                {
                    continue;
                }

#if (CINEMACHINE || CINEMACHINE_2 || APPMANA_CINEMACHINE_2 || CINEMACHINE_3 || APPMANA_CINEMACHINE_3) && ENABLE_INPUT_SYSTEM
                var zAxisRef = provider.zAxis;
                if (zAxisRef != null && IsScrollActionEnabledForPlayer(zAxisRef.action, m_Player))
                {
                    return true;
                }
#endif
            }

            for (var i = 0; i < m_ActionRefGetters.Count; i++)
            {
                var (owner, getter) = m_ActionRefGetters[i];
                if (owner == null || !owner.isActiveAndEnabled)
                {
                    continue;
                }

                var actionRef = getter();
                if (actionRef != null && IsScrollActionEnabledForPlayer(actionRef.action, m_Player))
                {
                    return true;
                }
            }

            for (var i = 0; i < m_ReflectedActionFields.Count; i++)
            {
                var (owner, field) = m_ReflectedActionFields[i];
                if (owner == null || !owner.isActiveAndEnabled)
                {
                    continue;
                }

                var action = ExtractInputAction(field.GetValue(owner));
                if (action != null && IsScrollActionEnabledForPlayer(action, m_Player))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPointerOverScrollableUgui(Vector2 pointerPosition)
        {
            var eventSystem = ResolveEventSystem();
            m_RaycastResults.Clear();

            if (eventSystem != null)
            {
                if (m_PointerEventData == null || m_CachedEventSystem != eventSystem)
                {
                    m_CachedEventSystem = eventSystem;
                    m_PointerEventData = new ExtendedPointerEventData(eventSystem);
                }

                m_PointerEventData.Reset();
                m_PointerEventData.position = pointerPosition;
                if (m_PointerDevice != null)
                {
                    m_PointerEventData.device = m_PointerDevice;
                }

                eventSystem.RaycastAll(m_PointerEventData, m_RaycastResults);
            }

            if (m_RaycastResults.Count == 0 && !Application.isPlaying)
            {
                FallbackEditModeGraphicRaycast(pointerPosition, eventSystem);
            }

            for (var i = 0; i < m_RaycastResults.Count; i++)
            {
                var hitGo = m_RaycastResults[i].gameObject;
                if (hitGo == null)
                {
                    continue;
                }

                // InputSystemUIInputModule dispatches OnScroll strictly to the topmost raycast hit's hierarchy.
                return IsGameObjectScrollableUgui(hitGo);
            }

            return false;
        }

        public bool IsGameObjectScrollableUgui(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return false;
            }

            var current = hitObject.transform;
            while (current != null)
            {
                m_ScrollHandlers.Clear();
                current.GetComponents(m_ScrollHandlers);

                for (var i = 0; i < m_ScrollHandlers.Count; i++)
                {
                    var handler = m_ScrollHandlers[i];
                    if (handler == null)
                    {
                        continue;
                    }

                    if (handler is Behaviour { isActiveAndEnabled: false })
                    {
                        continue;
                    }

                    if (handler is ScrollRect scrollRect)
                    {
                        if (CanScrollRectScroll(scrollRect))
                        {
                            return true;
                        }

                        continue;
                    }

#if TMP
                    if (handler is TMP_InputSystemInputField customTmp)
                    {
                        if (CanTmpInputFieldScroll(
                                customTmp.lineType == TMP_InputSystemInputField.LineType.SingleLine,
                                customTmp.textComponent,
                                customTmp.textViewport))
                        {
                            return true;
                        }

                        continue;
                    }

                    if (handler is TMPro.TMP_InputField builtinTmp)
                    {
                        if (CanTmpInputFieldScroll(
                                builtinTmp.lineType == TMPro.TMP_InputField.LineType.SingleLine,
                                builtinTmp.textComponent,
                                builtinTmp.textViewport))
                        {
                            return true;
                        }

                        continue;
                    }
#endif

                    if (handler is EventTrigger eventTrigger)
                    {
                        if (HasActiveScrollEventTrigger(eventTrigger))
                        {
                            return true;
                        }

                        continue;
                    }

                    if (handler is ObservableEventTrigger)
                    {
                        continue;
                    }

                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        public static bool CanScrollRectScroll(ScrollRect scrollRect)
        {
            if (scrollRect == null || !scrollRect.isActiveAndEnabled || scrollRect.content == null)
            {
                return false;
            }

            if (!scrollRect.vertical && !scrollRect.horizontal)
            {
                return false;
            }

            if (scrollRect.movementType == ScrollRect.MovementType.Unrestricted)
            {
                return true;
            }

            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();
            if (viewport == null)
            {
                return true;
            }

            var contentRect = scrollRect.content.rect;
            var viewportRect = viewport.rect;
            const float epsilon = 0.5f;

            if (scrollRect.vertical && contentRect.height > viewportRect.height + epsilon)
            {
                return true;
            }

            if (scrollRect.horizontal && contentRect.width > viewportRect.width + epsilon)
            {
                return true;
            }

            return false;
        }

        public static bool HasScrollBinding(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite)
                {
                    continue;
                }

                var path = !string.IsNullOrEmpty(binding.effectivePath) ? binding.effectivePath : binding.path;
                if (!string.IsNullOrEmpty(path) &&
                    path.IndexOf("/scroll", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsScrollActionEnabledForPlayer(InputAction action, RemotePlayableConfiguration player)
        {
            if (action == null)
            {
                return false;
            }

            var resolved = action;
            if (player != null && player.actions != null)
            {
                var mapped = player.actions.FindAction(action.id) ?? player.actions.FindAction(action.name);
                if (mapped != null)
                {
                    resolved = mapped;
                }
            }

            return resolved.enabled && HasScrollBinding(resolved);
        }

#if TMP
        private static bool CanTmpInputFieldScroll(
            bool isSingleLine,
            TMPro.TMP_Text textComponent,
            RectTransform textViewport)
        {
            if (isSingleLine || textComponent == null || textViewport == null)
            {
                return false;
            }

            const float epsilon = 0.5f;
            return textComponent.preferredHeight > textViewport.rect.height + epsilon;
        }
#endif

        private static bool HasActiveScrollEventTrigger(EventTrigger trigger)
        {
            if (trigger == null || !trigger.isActiveAndEnabled || trigger.triggers == null)
            {
                return false;
            }

            var entries = trigger.triggers;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null &&
                    entry.eventID == EventTriggerType.Scroll &&
                    entry.callback != null &&
                    entry.callback.GetPersistentEventCount() > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildActionCache()
        {
            m_CinemachineProviders.Clear();
            m_ActionRefGetters.Clear();
            m_ReflectedActionFields.Clear();
            m_ActionCacheDirty = false;

            var behaviours = UnityUtilities.FindObjectsByType<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is InputSystemUIInputModule
#if TMP
                    || behaviour is InputSystemTMPInputFieldModule
#endif
                   )
                {
                    continue;
                }

                if (m_Player != null)
                {
                    var ownerPlayer = behaviour.GetComponentInParent<RemotePlayableConfiguration>(true);
                    if (ownerPlayer != null && ownerPlayer != m_Player)
                    {
                        continue;
                    }
                }

                if (behaviour is CinemachinePressableInputProvider cmProvider)
                {
                    if (m_Player != null &&
                        cmProvider.remotePlayableConfiguration != null &&
                        cmProvider.remotePlayableConfiguration != m_Player)
                    {
                        continue;
                    }

                    m_CinemachineProviders.Add(cmProvider);
                    continue;
                }

                if (behaviour is IHasInputActionReferences hasRefs)
                {
                    var props = hasRefs.inputActionReferenceProperties;
                    if (props != null)
                    {
                        for (var p = 0; p < props.Length; p++)
                        {
                            if (props[p].get != null)
                            {
                                m_ActionRefGetters.Add((behaviour, props[p].get));
                            }
                        }
                    }

                    continue;
                }

                var actionFields = GetActionFieldsForType(behaviour.GetType());
                for (var f = 0; f < actionFields.Length; f++)
                {
                    m_ReflectedActionFields.Add((behaviour, actionFields[f]));
                }
            }
        }

        private static FieldInfo[] GetActionFieldsForType(Type type)
        {
            if (s_ActionFieldsByType.TryGetValue(type, out var cached))
            {
                return cached;
            }

            List<FieldInfo> matching = null;
            var current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(Behaviour) &&
                   current != typeof(Component) && current != typeof(object))
            {
                var fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++)
                {
                    var fieldType = fields[i].FieldType;
                    if (fieldType == typeof(InputActionReference) ||
                        fieldType == typeof(InputActionProperty) ||
                        fieldType == typeof(InputAction) ||
                        fieldType == typeof(MultiplayerInputActionReference))
                    {
                        matching ??= new List<FieldInfo>(2);
                        matching.Add(fields[i]);
                    }
                }

                current = current.BaseType;
            }

            var result = matching != null ? matching.ToArray() : Array.Empty<FieldInfo>();
            s_ActionFieldsByType[type] = result;
            return result;
        }

        private static InputAction ExtractInputAction(object fieldValue)
        {
            return fieldValue switch
            {
                null => null,
                InputActionReference actionRef => actionRef.action,
                InputActionProperty actionProp => actionProp.action,
                InputAction action => action,
                MultiplayerInputActionReference mpRef =>
                    (s_MultiplayerActionRefField?.GetValue(mpRef) as InputActionReference)?.action,
                _ => null
            };
        }

        private EventSystem ResolveEventSystem()
        {
            if (m_Player != null)
            {
                var playerEventSystem = m_Player.GetComponentInChildren<EventSystem>();
                if (playerEventSystem != null && playerEventSystem.isActiveAndEnabled)
                {
                    return playerEventSystem;
                }
            }

            return EventSystem.current;
        }

        private void FallbackEditModeGraphicRaycast(Vector2 pointerPosition, EventSystem eventSystem)
        {
            var pointerData = m_PointerEventData ?? new PointerEventData(eventSystem);
            pointerData.Reset();
            pointerData.position = pointerPosition;

            var raycasters = UnityUtilities.FindObjectsByType<GraphicRaycaster>(false);
            for (var i = 0; i < raycasters.Length; i++)
            {
                var raycaster = raycasters[i];
                if (raycaster != null && raycaster.isActiveAndEnabled)
                {
                    raycaster.Raycast(pointerData, m_RaycastResults);
                }
            }
        }

        private void OnSceneChanged(Scene scene, LoadSceneMode mode)
        {
            m_ActionCacheDirty = true;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            m_ActionCacheDirty = true;
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;
            SceneManager.sceneLoaded -= OnSceneChanged;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
}
