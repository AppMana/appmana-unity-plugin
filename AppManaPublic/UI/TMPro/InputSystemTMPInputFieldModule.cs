using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AppMana.ComponentModel;
using AppMana.InteractionToolkit;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Observable = UniRx.Observable;

namespace AppMana.UI.TMPro
{
    [DefaultExecutionOrder(-1000)]
    public class InputSystemTMPInputFieldModule : UIBehaviour
    {
        private static readonly ComponentCache m_ParentCache = new();
        [SerializeField] private InputActionAsset m_ActionsAsset;
        [SerializeField] private float m_RepeatDelaySeconds = 0.16f;
        [SerializeField] private float m_RepeatPeriodSeconds = 0.05f;
        private InputActionReference m_Backspace;
        private InputActionReference m_Copy;
        private InputActionReference m_Cut;
        private InputActionReference m_Deselect;
        private InputActionReference m_Paste;
        private InputActionReference m_SelectAll;
        private InputActionReference m_DeleteKey;
        private InputActionReference m_MoveDown;
        private InputActionReference m_MoveLeft;
        private InputActionReference m_MoveRight;
        private InputActionReference m_MoveUp;
        private InputActionReference m_MovePageDown;
        private InputActionReference m_MovePageUp;
        private InputActionReference m_MoveToEndOfLine;
        private InputActionReference m_MoveToStartOfLine;
#if TMP
        private ConditionalWeakTable<TMP_InputSystemInputField, InputSystemFieldState> m_States = new();

        private EventModifiers currentModifiers
        {
            get
            {
                var modifiers = EventModifiers.None;
                if (Keyboard.current?.altKey.isPressed ?? false)
                {
                    modifiers |= EventModifiers.Alt;
                }

                if (Keyboard.current?.shiftKey.isPressed ?? false)
                {
                    modifiers |= EventModifiers.Shift;
                }

                if ((Keyboard.current?.leftCommandKey.isPressed ?? false) ||
                    (Keyboard.current?.rightCommandKey.isPressed ?? false))
                {
                    modifiers |= EventModifiers.Command;
                }

                if (Keyboard.current?.ctrlKey.isPressed ?? false)
                {
                    modifiers |= EventModifiers.Control;
                }

                return modifiers;
            }
        }

        public bool PopEvent(TMP_InputSystemInputField sender, Event processingEvent)
        {
            if (!m_States.TryGetValue(sender, out var state))
            {
                return false;
            }

            if (state.eventQueue.TryDequeue(out var evt))
            {
                processingEvent.button = evt.button;
                processingEvent.character = evt.character;
                processingEvent.modifiers = evt.modifiers;
                processingEvent.commandName = evt.commandName;
                processingEvent.keyCode = evt.keyCode;
                processingEvent.rawType = evt.rawType;
                return true;
            }

            if (state.lastFrame == Time.frameCount)
            {
                return false;
            }

            processingEvent.rawType = EventType.MouseDown;
            state.lastFrame = Time.frameCount;
            return true;
        }

        public void OnDisabledField(TMP_InputSystemInputField tmpInputSystemInputField)
        {
            if (m_States.TryGetValue(tmpInputSystemInputField, out var state))
            {
                state.disposable.Dispose();
                state.unsub();
                state.unsub = null;
                state.disposable = new CompositeDisposable();
            }
        }

        public void OnEnabledField(TMP_InputSystemInputField tmpInputSystemInputField)
        {
            if (!Application.isPlaying)
            {
                throw new UnityException($"unexpectedly called {nameof(OnEnabledField)}");
            }
            
            var state = new InputSystemFieldState();
            m_States.AddOrUpdate(tmpInputSystemInputField, state);
            var compositeDisposable = state.disposable;

            // mappings
            void Subscribe(InputActionReference inputActionRef, KeyCode sendKeyCode, bool repeats)
            {
                if (inputActionRef == null)
                {
                    throw new UnityException("input action ref was unexpectedly null");
                }

                var action = inputActionRef.action;
                if (!action.enabled)
                {
                    action.Enable();
                }

                var performed = action.OnPerformedAsObservable()
                    // whenever the key is pressed
                    .Where(ctx => ctx.action is
                        { activeControl: KeyControl { wasPressedThisFrame: true }, phase: InputActionPhase.Performed });
                var withRepeats = repeats
                    ? // repeat it when it's held
                    performed.SelectMany(ctx =>
                    {
                        return Observable.Return((ctx, updateSelected: false))
                            .Concat(Observable.Timer(TimeSpan.FromSeconds(m_RepeatDelaySeconds),
                                TimeSpan.FromSeconds(m_RepeatPeriodSeconds)).Select(_ => (ctx, updateSelected: true)))
                            // repeat until the key is released
                            .TakeUntil(action
                                .OnPerformedAsObservable()
                                .Where(ctx2 => ctx2.action is
                                    { activeControl: KeyControl { wasReleasedThisFrame: true } }));
                    })
                    : performed.Select(ctx => { return (ctx, updateSelected: false); });
                withRepeats.Subscribe(tuple =>
                    {
                        var (_, updateSelected) = tuple;
                        var evt = new Event()
                        {
                            character = (char)0,
                            keyCode = sendKeyCode,
                            rawType = EventType.KeyDown,
                            modifiers = currentModifiers
                        };

                        state.eventQueue.Enqueue(evt);
                        // repeats do not cause OnUpdateSelected to be raised, so we'll call process event directly
                        if (updateSelected)
                        {
                            // tmpInputSystemInputField.ProcessEvent(evt);
                            ExecuteEvents.Execute(tmpInputSystemInputField.gameObject,
                                new BaseEventData(EventSystem.current), ExecuteEvents.updateSelectedHandler);
                        }
                    })
                    .AddTo(compositeDisposable);
            }

            void HandleChar(char character)
            {
                if (character < 32 || character > 127)
                {
                    return;
                }
                state.eventQueue.Enqueue(new Event()
                {
                    button = 0,
                    character = character,
                    commandName = "",
                    rawType = EventType.KeyDown,
                    modifiers = currentModifiers
                });
            }

            var keyboardSubs = new Dictionary<Keyboard, IDisposable>();

            // keyboard inputs
            // append
            // todo: multiplayer
            void HandleDeviceChange(InputDevice device, InputDeviceChange change)
            {
                if (device is Keyboard keyboard)
                {
                    switch (change)
                    {
                        case InputDeviceChange.Added:
                            if (keyboardSubs.ContainsKey(keyboard))
                            {
                                break;
                            }
                            
                            var disposable = new CompositeDisposable();
                            keyboardSubs[keyboard] = disposable;
                            keyboard.OnTextInputAsObservable()
                                .Subscribe(HandleChar)
                                .AddTo(disposable)
                                .AddTo(compositeDisposable);
                            break;
                        case InputDeviceChange.Removed:
                            if (!keyboardSubs.ContainsKey(keyboard))
                            {
                                break;
                            }

                            keyboardSubs.Remove(keyboard, out var sub);
                            sub.Dispose();
                            break;
                    }
                }
            }

            if (Keyboard.current != null)
            {
                HandleDeviceChange(Keyboard.current, InputDeviceChange.Added);
            }

            InputSystem.onDeviceChange += HandleDeviceChange;
            state.unsub = () => InputSystem.onDeviceChange -= HandleDeviceChange;

            // everything else
            Subscribe(m_Backspace, KeyCode.Backspace, true);
            Subscribe(m_DeleteKey, KeyCode.Delete, true);
            Subscribe(m_MoveToStartOfLine, KeyCode.Home, false);
            Subscribe(m_MoveToEndOfLine, KeyCode.End, false);
            Subscribe(m_SelectAll, KeyCode.A, false);
            Subscribe(m_Copy, KeyCode.C, false);
            Subscribe(m_Paste, KeyCode.V, false);
            Subscribe(m_Cut, KeyCode.X, false);
            Subscribe(m_MoveLeft, KeyCode.LeftArrow, true);
            Subscribe(m_MoveRight, KeyCode.RightArrow, true);
            Subscribe(m_MoveUp, KeyCode.UpArrow, true);
            Subscribe(m_MoveDown, KeyCode.DownArrow, true);
            Subscribe(m_MovePageUp, KeyCode.PageUp, true);
            Subscribe(m_MovePageDown, KeyCode.PageDown, true);
            Subscribe(m_Deselect, KeyCode.Escape, false);

            compositeDisposable.AddTo(this);
        }

        public static InputSystemTMPInputFieldModule ParentInstance(Component thisComponent) =>
            m_ParentCache.ComputeIfAbsent(thisComponent,
                _ => thisComponent.GetComponentInParent<InputSystemTMPInputFieldModule>() ??
                     UnityUtilities.FindFirstObjectByType<InputSystemTMPInputFieldModule>());

        public InputActionAsset actionsAsset
        {
            get => m_ActionsAsset;
            set
            {
                m_ActionsAsset = value;
                AssignBasedOnNames();
            }
        }

        private void AssignBasedOnNames()
        {
            m_Backspace = FindReference(nameof(m_Backspace));
            m_Copy = FindReference(nameof(m_Copy));
            m_Cut = FindReference(nameof(m_Cut));
            m_Deselect = FindReference(nameof(m_Deselect));
            m_Paste = FindReference(nameof(m_Paste));
            m_SelectAll = FindReference(nameof(m_SelectAll));
            m_DeleteKey = FindReference(nameof(m_DeleteKey));
            m_MoveDown = FindReference(nameof(m_MoveDown));
            m_MoveLeft = FindReference(nameof(m_MoveLeft));
            m_MoveRight = FindReference(nameof(m_MoveRight));
            m_MoveUp = FindReference(nameof(m_MoveUp));
            m_MovePageDown = FindReference(nameof(m_MovePageDown));
            m_MovePageUp = FindReference(nameof(m_MovePageUp));
            m_MoveToEndOfLine = FindReference(nameof(m_MoveToEndOfLine));
            m_MoveToStartOfLine = FindReference(nameof(m_MoveToStartOfLine));
        }

        private InputActionReference FindReference(string name)
        {
            if (name.StartsWith("m_"))
            {
                name = name.Substring("m_".Length);
            }

            return InputActionReference.Create(m_ActionsAsset.actionMaps
                .SelectMany(map => map.actions)
                .FirstOrDefault(action =>
                    string.Equals(action.name, name, StringComparison.InvariantCultureIgnoreCase)));
        }

        private void SetDefaultActionsAsset()
        {
            var defaultActions = new InputActions();
            m_ActionsAsset = defaultActions.asset;
            m_Backspace = InputActionReference.Create(defaultActions.TextInput.Backspace);
            m_Copy = InputActionReference.Create(defaultActions.TextInput.Copy);
            m_Cut = InputActionReference.Create(defaultActions.TextInput.Cut);
            m_Deselect = InputActionReference.Create(defaultActions.TextInput.Deselect);
            m_Paste = InputActionReference.Create(defaultActions.TextInput.Paste);
            m_DeleteKey = InputActionReference.Create(defaultActions.TextInput.DeleteKey);
            m_MoveDown = InputActionReference.Create(defaultActions.TextInput.MoveDown);
            m_MoveLeft = InputActionReference.Create(defaultActions.TextInput.MoveLeft);
            m_MoveRight = InputActionReference.Create(defaultActions.TextInput.MoveRight);
            m_MoveUp = InputActionReference.Create(defaultActions.TextInput.MoveUp);
            m_SelectAll = InputActionReference.Create(defaultActions.TextInput.SelectAll);
            m_MovePageDown = InputActionReference.Create(defaultActions.TextInput.MovePageDown);
            m_MovePageUp = InputActionReference.Create(defaultActions.TextInput.MovePageUp);
            m_MoveToEndOfLine = InputActionReference.Create(defaultActions.TextInput.MoveToEndOfLine);
            m_MoveToStartOfLine = InputActionReference.Create(defaultActions.TextInput.MoveToStartOfLine);
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_ActionsAsset == null)
            {
                SetDefaultActionsAsset();
            }
            else
            {
                AssignBasedOnNames();
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetDefaultActionsAsset();
        }
#endif

        private void LateUpdate()
        {
            foreach (var (_, state) in m_States)
            {
                state.eventQueue.Clear();
            }
        }

#endif
    }

    internal class InputSystemFieldState
    {
        internal CompositeDisposable disposable = new();
        internal Queue<Event> eventQueue = new();
        internal int lastFrame = Int32.MinValue;
        internal Action unsub;
    }
}