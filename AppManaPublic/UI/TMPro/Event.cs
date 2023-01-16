using System;
using UnityEngine;

namespace AppMana.UI.TMPro
{
    [Flags]
    public enum EventModifiers
    {
        None = 0,
        Shift = 1,
        Alt = 2,
        Command = 4,
        Control = 8
    }

    public enum EventType
    {
        KeyUp,
        KeyDown,
        ValidateCommand,
        ExecuteCommand,
        MouseDown
    }

    public enum KeyCode
    {
        NoneOrOther = 0,
        Backspace,
        Delete,
        Home,
        End,
        A,
        C,
        V,
        X,
        LeftArrow,
        RightArrow,
        UpArrow,
        DownArrow,
        PageUp,
        PageDown,
        Return,
        KeypadEnter,
        Escape
    }

    public class Event
    {
        public const string SelectAllCommand = "SelectAll";
        public EventType rawType;
        public string commandName;
        public KeyCode keyCode;
        public char character;
        public EventModifiers modifiers;
        public int button;

        public static bool PopEvent(TMP_InputSystemInputField caller, Event processingEvent)
        {
            var inputSystemTMPInputFieldModule = InputSystemTMPInputFieldModule.ParentInstance(caller);
            if (inputSystemTMPInputFieldModule == null)
            {
                return false;
            }
            return inputSystemTMPInputFieldModule.PopEvent(caller, processingEvent);
        }
    }
}